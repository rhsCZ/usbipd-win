// SPDX-FileCopyrightText: 2022 Frans van Dorsselaer
//
// SPDX-License-Identifier: GPL-3.0-only

using static Usbipd.ConsoleTools;

namespace Usbipd;

sealed partial class CommandHandlers : ICommandHandlers
{
    async Task<ExitCode> ICommandHandlers.Server(string[] args, IConsole console, CancellationToken cancellationToken)
    {
        // Pre-conditions that may fail due to user mistakes. Fail gracefully...

        if (!CheckInstalled(console))
        {
            return ExitCode.Failure;
        }
        if (!CheckWriteAccess(console))
        {
            return ExitCode.AccessDenied;
        }

        using var mutex = new Mutex(true, Server.SingletonMutexName, out var createdNew);
        if (!createdNew)
        {
            console.ReportError("Another instance is already running.");
            return ExitCode.Failure;
        }

        // From here on, the server should run without error. Any further errors (exceptions) are probably bugs...

        using var host = Host.CreateDefaultBuilder()
            .UseWindowsService()
            .ConfigureAppConfiguration((context, builder) =>
            {
                var defaultConfig = new Dictionary<string, string?>
                {
                    // Our ETW logger is smart enough to only log events when a listener is attached, so we can safely log Trace level events.
                    { $"Logging:Etw:LogLevel:{nameof(Usbipd)}", "Trace" }
                };
                // set the above as defaults
                _ = builder.AddInMemoryCollection(defaultConfig);
                // allow overrides from the environment
                _ = builder.AddEnvironmentVariables();
                // allow overrides from the command line
                _ = builder.AddCommandLine(args);
            })
            .ConfigureLogging((context, logging) => _ = logging.AddEtwLogger())
            .ConfigureServices((hostContext, services) =>
            {
                _ = services.AddHostedService<Server>();
                _ = services.AddSingleton<PcapNg>();
                _ = services.AddScoped<ClientContext>();
                _ = services.AddScoped<ConnectedClient>();
                _ = services.AddScoped<AttachedClient>();
            })
            .Build();

        await host.RunAsync(cancellationToken);
        return ExitCode.Success;
    }
}

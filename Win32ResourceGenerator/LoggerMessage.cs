// SPDX-FileCopyrightText: 2026 Frans van Dorsselaer
//
// SPDX-License-Identifier: GPL-3.0-only

using System.Diagnostics.Tracing;
using Microsoft.CodeAnalysis;

namespace Win32ResourceGenerator;

public record LoggerMessage(
    ushort Id,
    string Name,
    EventLevel Level,
    string Message,
    Location Location);

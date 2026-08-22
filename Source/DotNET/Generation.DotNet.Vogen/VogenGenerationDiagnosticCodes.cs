// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.Vogen;

/// <summary>
/// Stable diagnostic codes emitted by Vogen concept discovery.
/// </summary>
public static class VogenGenerationDiagnosticCodes
{
    /// <summary>
    /// A Vogen concept has a backing type that Screenplay generation cannot represent as a primitive.
    /// </summary>
    public const string UnsupportedBackingType = "VOG0001";
}

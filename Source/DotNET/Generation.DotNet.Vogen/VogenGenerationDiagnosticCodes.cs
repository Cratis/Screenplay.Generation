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

    /// <summary>
    /// A Vogen concept normalizes input in a way Screenplay concept validation cannot preserve.
    /// </summary>
    public const string InputNormalizationNotRepresented = "VOG0002";

    /// <summary>
    /// A Vogen concept declares a named instance that Screenplay concepts cannot preserve.
    /// </summary>
    public const string NamedInstanceNotRepresented = "VOG0003";

    /// <summary>
    /// Applicable Vogen source lacks an authoritative stable source mapping.
    /// </summary>
    public const string UnsafeSourceMapping = "VOG0004";
}

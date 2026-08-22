// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.Vogen;

/// <summary>
/// Defines the exact Roslyn metadata names interpreted by the Vogen adapter.
/// </summary>
public static class VogenMetadataNames
{
    /// <summary>
    /// The non-generic value-object attribute. Its first constructor argument is the optional backing type.
    /// </summary>
    public const string ValueObjectAttribute = "Vogen.ValueObjectAttribute";

    /// <summary>
    /// The generic value-object attribute. Its first type argument is the backing type.
    /// </summary>
    public const string GenericValueObjectAttribute = "Vogen.ValueObjectAttribute`1";

    /// <summary>
    /// The assembly defaults attribute. Its first constructor argument is the optional default backing type.
    /// </summary>
    public const string DefaultsAttribute = "Vogen.VogenDefaultsAttribute";
}

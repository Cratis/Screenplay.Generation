// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.Vogen;

/// <summary>
/// Defines the exact Vogen API capabilities understood by the Vogen concept adapter.
/// </summary>
public static class VogenAdapterApiCapabilities
{
    /// <summary>
    /// A Vogen value-object declaration API, proven by either the generic or non-generic declaration attribute.
    /// </summary>
    public static AdapterApiCapability ValueObjectDeclaration { get; } = new() { Id = "vogen.value-object-declaration" };

    /// <summary>
    /// The optional Vogen validation-result API used to extract authored validation messages.
    /// </summary>
    public static AdapterApiCapability ValidationResult { get; } = new() { Id = "vogen.validation-result" };
}

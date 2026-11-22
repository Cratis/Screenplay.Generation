// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet;

/// <summary>
/// Selects the declared or effective base-class property surface.
/// </summary>
public enum DotNetMemberSurfaceScope
{
    /// <summary>
    /// Selects only properties declared on the nominated type.
    /// </summary>
    Declared = 0,

    /// <summary>
    /// Selects properties derived-first through the base-class chain, excluding interfaces and, by default, non-authored declarations.
    /// </summary>
    Inherited = 1
}

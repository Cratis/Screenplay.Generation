// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet;

/// <summary>
/// Selects eligible property origins within an inherited member surface without changing artifact admission or source authority.
/// </summary>
public enum DotNetMemberOriginPolicy
{
    /// <summary>
    /// Selects only properties proven to originate in analyzed authored syntax trees.
    /// </summary>
    AuthoredOnly = 0,

    /// <summary>
    /// Also selects properties from metadata, non-authored, and generated declarations.
    /// </summary>
    AllOrigins = 1
}

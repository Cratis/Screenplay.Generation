// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet;

/// <summary>
/// The exception that is thrown when legacy authored syntax trees have no unique stable path identity.
/// </summary>
public sealed class DotNetAuthoredSyntaxTreeIdentityNotUnique()
    : Exception("Legacy authored syntax trees must have unique non-empty paths for deterministic enumeration");

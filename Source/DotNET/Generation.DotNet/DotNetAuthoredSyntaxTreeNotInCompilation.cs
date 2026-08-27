// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet;

/// <summary>
/// The exception that is thrown when a host-authoritative authored syntax tree is absent from its project compilation.
/// </summary>
/// <param name="path">The syntax-tree path.</param>
public sealed class DotNetAuthoredSyntaxTreeNotInCompilation(string path)
    : Exception($"The authored syntax tree '{path}' is not present in the project compilation");

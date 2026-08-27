// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Validates that an authored source range belongs to source the host has declared authoritative.
/// </summary>
/// <remarks>
/// Implementations are supplied by a source-language host. The contract deliberately has no dependency on Roslyn
/// or any other source platform.
/// </remarks>
public interface ISourceAuthorityValidator
{
    /// <summary>
    /// Gets whether a source range belongs to authoritative authored source.
    /// </summary>
    /// <param name="source">The source range to validate.</param>
    /// <returns><see langword="true"/> when the source is authoritative; otherwise, <see langword="false"/>.</returns>
    bool IsAuthoritative(SourceRange source);
}

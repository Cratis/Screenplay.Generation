// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Defines stable diagnostics produced by the framework-neutral generation pipeline.
/// </summary>
public static class GenerationDiagnosticCodes
{
    /// <summary>
    /// Two adapters or source locations asserted incompatible definitions for the same artifact.
    /// </summary>
    public const string ConflictingArtifact = "GEN0001";

    /// <summary>
    /// Two assertions reuse one fact identity for different semantic facts.
    /// </summary>
    public const string ConflictingFactIdentity = "GEN0003";

    /// <summary>
    /// A recognized artifact cannot yet be represented by the Screenplay lowerer.
    /// </summary>
    public const string UnsupportedArtifact = "GEN0004";

    /// <summary>
    /// The generated Screenplay document did not compile.
    /// </summary>
    public const string DocumentDidNotCompile = "GEN0005";

    /// <summary>
    /// Two adapters or source locations asserted incompatible definitions for the same relationship.
    /// </summary>
    public const string ConflictingRelationship = "GEN0006";

    /// <summary>
    /// Artifacts assigned incompatible slice kinds to the same module, feature, and slice name.
    /// </summary>
    public const string ConflictingSliceKind = "GEN0007";

    /// <summary>
    /// Two adapters or source locations assigned incompatible placements to the same artifact role.
    /// </summary>
    public const string ConflictingPlacement = "GEN0008";

    /// <summary>
    /// A recognized artifact lacks relationships required to represent it faithfully.
    /// </summary>
    public const string IncompleteArtifact = "GEN0009";

    /// <summary>
    /// The generated document changed after compile and canonical reprint.
    /// </summary>
    public const string UnstableRoundTrip = "GEN0010";
}

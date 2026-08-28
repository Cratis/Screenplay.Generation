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

    /// <summary>
    /// Incompatible representations were asserted for one concept subject.
    /// </summary>
    public const string ConflictingConceptRepresentation = "GEN0011";

    /// <summary>
    /// A concept artifact has no proven representation and cannot be emitted.
    /// </summary>
    public const string MissingConceptRepresentation = "GEN0012";

    /// <summary>
    /// A concept representation is internally invalid or unsupported by Screenplay.
    /// </summary>
    public const string UnsupportedConceptRepresentation = "GEN0013";

    /// <summary>
    /// Distinct concept subjects require the same Screenplay declaration name.
    /// </summary>
    public const string ConflictingConceptName = "GEN0015";

    /// <summary>
    /// A subject-aware type reference targets a concept that could not be emitted.
    /// </summary>
    public const string MissingConceptReference = "GEN0016";

    /// <summary>
    /// A concept fact's subject does not match the concept subject in its definition.
    /// </summary>
    public const string InvalidConceptFact = "GEN0017";

    /// <summary>
    /// Incompatible definitions were asserted for one named concept attribute.
    /// </summary>
    public const string ConflictingConceptAttribute = "GEN0018";

    /// <summary>
    /// A concept attribute cannot be represented safely by Screenplay.
    /// </summary>
    public const string UnsupportedConceptAttribute = "GEN0019";

    /// <summary>
    /// Incompatible definitions were asserted for one concept validation rule identity.
    /// </summary>
    public const string ConflictingConceptValidationRule = "GEN0020";

    /// <summary>
    /// A concept validation rule cannot be represented safely by Screenplay.
    /// </summary>
    public const string UnsupportedConceptValidationRule = "GEN0021";

    /// <summary>
    /// An artifact fact or placement uses an unknown or undefined artifact kind.
    /// </summary>
    public const string UnsupportedArtifactKind = "GEN0022";

    /// <summary>
    /// An artifact placement uses an unknown or undefined slice kind.
    /// </summary>
    public const string UnsupportedSliceKind = "GEN0023";

    /// <summary>
    /// A relationship fact uses an unknown or undefined relationship kind.
    /// </summary>
    public const string UnsupportedRelationshipKind = "GEN0024";

    /// <summary>
    /// A concept representation fact uses an unknown or undefined representation kind.
    /// </summary>
    public const string UnsupportedConceptRepresentationKind = "GEN0025";

    /// <summary>
    /// A concept representation fact uses an unknown or undefined primitive kind.
    /// </summary>
    public const string UnsupportedPrimitiveKind = "GEN0026";

    /// <summary>
    /// A concept attribute fact uses an unknown or undefined attribute kind.
    /// </summary>
    public const string UnsupportedConceptAttributeKind = "GEN0027";

    /// <summary>
    /// A concept validation fact uses an unknown or undefined validation rule kind.
    /// </summary>
    public const string UnsupportedConceptValidationRuleKind = "GEN0028";

    /// <summary>
    /// A fact uses an unknown or undefined evidence strength.
    /// </summary>
    public const string UnsupportedEvidenceStrength = "GEN0029";

    /// <summary>
    /// A specification step fact uses an unknown or undefined authored phase.
    /// </summary>
    public const string UnsupportedSpecificationStepPhase = "GEN0030";

    /// <summary>
    /// A specification step fact uses an unknown or undefined behavior kind.
    /// </summary>
    public const string UnsupportedSpecificationStepKind = "GEN0031";

    /// <summary>
    /// A specification value fact uses an unknown or undefined value kind.
    /// </summary>
    public const string UnsupportedSpecificationValueKind = "GEN0032";

    /// <summary>
    /// Incompatible definitions were asserted for one specification scenario.
    /// </summary>
    public const string ConflictingSpecificationScenario = "GEN0033";

    /// <summary>
    /// Incompatible definitions were asserted for one ordered specification step.
    /// </summary>
    public const string ConflictingSpecificationStep = "GEN0034";

    /// <summary>
    /// Incompatible definitions were asserted for one specification value path.
    /// </summary>
    public const string ConflictingSpecificationValue = "GEN0035";

    /// <summary>
    /// A specification scenario, step, or value fact is internally invalid.
    /// </summary>
    public const string InvalidSpecificationFact = "GEN0036";

    /// <summary>
    /// A specification scenario cannot be admitted atomically because required steps, values, artifacts, or placements are missing or conflicted.
    /// </summary>
    public const string IncompleteSpecificationScenario = "GEN0037";

    /// <summary>
    /// A complete neutral specification uses behavior the current Screenplay syntax cannot represent exactly.
    /// </summary>
    public const string UnsupportedSpecificationLowering = "GEN0038";

    /// <summary>
    /// A recognized relationship did not contribute to emitted Screenplay syntax.
    /// </summary>
    public const string UnsupportedRelationship = "GEN0039";

    /// <summary>
    /// An admitted fact was omitted without a more specific pipeline diagnostic.
    /// </summary>
    public const string OmittedGenerationFact = "GEN0040";

    /// <summary>
    /// An admitted fact could not be classified by the generation pipeline.
    /// </summary>
    public const string UnclassifiedGenerationFact = "GEN0041";

    /// <summary>
    /// An admitted fact participated in a conflict without a more specific pipeline diagnostic.
    /// </summary>
    public const string ConflictingGenerationFact = "GEN0042";

    /// <summary>
    /// A member type use names an artifact owner that was not declared in the fixed base snapshot.
    /// </summary>
    public const string MissingTypeUseOwner = "GEN0043";

    /// <summary>
    /// A member type use names a member that was not declared in the fixed base snapshot.
    /// </summary>
    public const string MissingTypeUseMember = "GEN0044";

    /// <summary>
    /// An observed exact type subject has no declared artifact target in the fixed base snapshot.
    /// </summary>
    public const string MissingTypeUseTarget = "GEN0045";

    /// <summary>
    /// Incompatible exact type uses were asserted for one artifact member.
    /// </summary>
    public const string ConflictingMemberTypeUse = "GEN0046";

    /// <summary>
    /// An exact observed type subject resolves to incompatible artifact targets.
    /// </summary>
    public const string ConflictingTypeUseTarget = "GEN0047";

    /// <summary>
    /// Incompatible artifact declarations prevent an exact type-use binding.
    /// </summary>
    public const string ConflictingTypeUseDeclaration = "GEN0048";

    /// <summary>
    /// An exact type-use shape cannot be represented without semantic loss.
    /// </summary>
    public const string UnsupportedTypeUseShape = "GEN0049";

    /// <summary>
    /// Incompatible declarations or roles were asserted for one artifact member.
    /// </summary>
    public const string ConflictingArtifactMember = "GEN0050";

    /// <summary>
    /// An artifact member lacks the declaration or exact type use required for safe lowering.
    /// </summary>
    public const string IncompleteArtifactMember = "GEN0051";

    /// <summary>
    /// A member type use contains an unknown or undefined shape node.
    /// </summary>
    public const string UnsupportedTypeUseShapeKind = "GEN0052";

    /// <summary>
    /// A member role fact contains an unknown or undefined role.
    /// </summary>
    public const string UnsupportedArtifactMemberRoleKind = "GEN0053";

    /// <summary>
    /// A granular fact's asserted subject does not equal its nested artifact owner.
    /// </summary>
    public const string InvalidGranularFactOwnership = "GEN0054";
}

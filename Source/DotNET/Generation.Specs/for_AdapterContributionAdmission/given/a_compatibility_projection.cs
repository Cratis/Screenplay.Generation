// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission.given;

public class a_compatibility_projection : a_contribution
{
    protected static ArtifactMemberTypeUseFact Observation(IReadOnlyList<TypeUseNodeObservation>? nodes = null) => EveryFact().OfType<ArtifactMemberTypeUseFact>().Single() with
    {
        Definition = new()
        {
            Member = EveryFact().OfType<ArtifactMemberTypeUseFact>().Single().Definition.Member,
            Type = new()
            {
                Name = "External", ObservedTypeSubject = ExternalSubject,
                Shape = [TypeUseShapeKind.Collection, TypeUseShapeKind.Collection, TypeUseShapeKind.Named],
                Nodes = nodes ?? Nodes()
            }
        }
    };

    protected static TypeUseNodeObservation[] Nodes() =>
    [
        new() { Name = "List" },
        new() { Name = "List", Subject = Subject("container") },
        new() { Name = "External", Subject = ExternalSubject }
    ];

    protected static TypeUseCompatibilityProjectionFact Projection() => new()
    {
        Id = Id("compatibility-projection"), Subject = ArtifactSubject, Evidence = Evidence(),
        Definition = new()
        {
            TypeUse = Observation().Id, Profile = TypeUseCompatibilityProfile.OneLayerCollection, Version = 1,
            SourceNodeIndex = 1,
            Type = new() { Name = "List", ObservedTypeSubject = Subject("container"), Shape = [TypeUseShapeKind.Collection, TypeUseShapeKind.Named] }
        }
    };

    protected static AdapterContributionAdmissionResult AdmitProjection(TypeUseCompatibilityProjectionFact? projection = null, GenerationFact? observation = null) =>
        Admit(descriptor: Descriptor([.. AllCapabilities(), GenerationFactCapability.TypeUseCompatibilityProjection]), contribution: Contribution([observation ?? Observation(), projection ?? Projection()]));
}

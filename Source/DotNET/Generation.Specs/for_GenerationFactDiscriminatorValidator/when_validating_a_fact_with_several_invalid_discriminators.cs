// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Screenplay.Generation.for_GenerationFactDiscriminatorValidator;

public class when_validating_a_fact_with_several_invalid_discriminators : Specification
{
    string[] _diagnostics = null!;

    void Because()
    {
        var subject = new SubjectId { Value = "dotnet://Invalid/artifact" };
        var fact = new ArtifactFact
        {
            Id = new FactId { Value = "artifact" },
            Subject = subject,
            Evidence = new Evidence
            {
                Adapter = new AdapterIdentity { Id = "first", Version = "1.0.0" },
                Strength = EvidenceStrength.Unknown
            },
            Definition = new ArtifactDefinition
            {
                Key = new ArtifactKey { Subject = subject, Kind = ArtifactKind.Unknown },
                Name = "Artifact",
                Properties =
                [
                    new PropertyDefinition { Name = "second", Type = new TypeReferenceDefinition { Name = "B", TargetArtifactKind = (ArtifactKind)92 } },
                    new PropertyDefinition { Name = "first", Type = new TypeReferenceDefinition { Name = "A", TargetArtifactKind = (ArtifactKind)91 } }
                ]
            }
        };

        var validator = typeof(GenerationResolver).Assembly.GetType("Cratis.Screenplay.Generation.GenerationFactDiscriminatorValidator");
        var result = validator.GetMethod("Validate", BindingFlags.Public | BindingFlags.Static).Invoke(null, [new GenerationFact[] { fact }]);
        var diagnostics = (GenerationDiagnostic[])result.GetType().GetProperty("Diagnostics").GetValue(result);
        _diagnostics = [.. diagnostics.Select(_ => $"{_.Code}|{_.Outcome}|{_.Message}")];
    }

    [Fact]
    void should_accumulate_in_validation_order_not_sorted_order() => _diagnostics.ShouldEqual(
    [
        "GEN0029|Unknown|Fact 'artifact' from adapter 'first' uses unknown EvidenceStrength value '-1'; the affected fact was omitted",
        "GEN0022|Unknown|Fact 'artifact' from adapter 'first' uses unknown ArtifactKind value '-1'; the affected fact was omitted",
        "GEN0022|Unsupported|Fact 'artifact' from adapter 'first' uses undefined ArtifactKind value '92'; the affected fact was omitted",
        "GEN0022|Unsupported|Fact 'artifact' from adapter 'first' uses undefined ArtifactKind value '91'; the affected fact was omitted"
    ]);
}

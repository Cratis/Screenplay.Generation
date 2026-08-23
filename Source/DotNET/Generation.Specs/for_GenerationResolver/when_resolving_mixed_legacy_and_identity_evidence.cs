// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_resolving_mixed_legacy_and_identity_evidence : given.facts
{
    const char Separator = '\u001f';

    ResolvedApplicationGraph _forward = null!;
    ResolvedApplicationGraph _reverse = null!;

    void Because()
    {
        var identity = IdentityFact();
        var legacy = LegacyFactCollidingWith(identity);
        _forward = new GenerationResolver().Resolve([Contribution(FirstAdapter, legacy, identity)]);
        _reverse = new GenerationResolver().Resolve([Contribution(FirstAdapter, identity, legacy)]);
    }

    [Fact] void should_retain_both_structurally_distinct_pieces_of_evidence() => _forward.Artifacts.Single().Variants.Single().Evidence.Count.ShouldEqual(2);
    [Fact] void should_order_mixed_evidence_independently_of_input_order() => JsonSerializer.Serialize(_reverse.Artifacts.Single().Variants.Single().Evidence).ShouldEqual(JsonSerializer.Serialize(_forward.Artifacts.Single().Variants.Single().Evidence));

    static ArtifactFact IdentityFact()
    {
        var legacyVersion = $"legacy{Separator}version";
        var legacyPath = $"legacy{Separator}Display.cs";
        var adapterId = string.Join(Separator, "identity", legacyVersion, "Exact", legacyPath, "1", "1", "1", "10", "tail");

        return new()
        {
            Id = new FactId { Value = "identity" },
            Subject = EventSubject,
            Definition = EventDefinition(),
            Evidence = new Evidence
            {
                Adapter = new AdapterIdentity { Id = adapterId, Version = $"identity{Separator}version" },
                Strength = EvidenceStrength.Exact,
                Source = new SourceRange
                {
                    Path = $"identity{Separator}Display.cs",
                    FileIdentity = new SourceFileIdentity
                    {
                        Project = $"Banking{Separator}Identity",
                        Path = $"Common{Separator}Order.cs"
                    },
                    StartLine = 2,
                    StartColumn = 3,
                    EndLine = 4,
                    EndColumn = 5
                },
                Explanation = $"identity{Separator}explanation"
            }
        };
    }

    static ArtifactFact LegacyFactCollidingWith(ArtifactFact identity)
    {
        var identityEvidence = identity.Evidence;
        var identitySource = identityEvidence.Source!;
        var identityFile = identitySource.FileIdentity!;
        var legacyAdapterId = $"{identityEvidence.Adapter.Id.Length.ToString(CultureInfo.InvariantCulture)}:identity";
        var legacyExplanation = string.Join(
            Separator,
            "tail",
            Encode(identityEvidence.Adapter.Version),
            Encode(identityEvidence.Strength.ToString()),
            Encode(identitySource.Path),
            Encode(identitySource.StartLine.ToString(CultureInfo.InvariantCulture)),
            Encode(identitySource.StartColumn.ToString(CultureInfo.InvariantCulture)),
            Encode(identitySource.EndLine.ToString(CultureInfo.InvariantCulture)),
            Encode(identitySource.EndColumn.ToString(CultureInfo.InvariantCulture)),
            Encode(identityEvidence.Explanation),
            Encode(identityFile.Project),
            Encode(identityFile.Path));

        return new()
        {
            Id = new FactId { Value = "legacy" },
            Subject = EventSubject,
            Definition = EventDefinition(),
            Evidence = new Evidence
            {
                Adapter = new AdapterIdentity { Id = legacyAdapterId, Version = $"legacy{Separator}version" },
                Strength = EvidenceStrength.Exact,
                Source = new SourceRange
                {
                    Path = $"legacy{Separator}Display.cs",
                    StartLine = 1,
                    StartColumn = 1,
                    EndLine = 1,
                    EndColumn = 10
                },
                Explanation = legacyExplanation
            }
        };
    }

    static string Encode(string? value) => value is null
        ? "-1:"
        : $"{value.Length.ToString(CultureInfo.InvariantCulture)}:{value}";
}

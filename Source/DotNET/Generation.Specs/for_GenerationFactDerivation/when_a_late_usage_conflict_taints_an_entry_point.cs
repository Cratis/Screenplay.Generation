// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_a_late_usage_conflict_taints_an_entry_point : given.an_artifact_usage
{
    GenerationDerivationSnapshot _result;
    GenerationDerivationSnapshot _reversed;
    ArtifactKey _unrelated;

    void Because()
    {
        var lateDeclaration = Declaration with { Value = Declaration.Value.Replace("FindCustomer", "ZindCustomer", StringComparison.Ordinal) };
        var firstMapping = UsageDeclaration("late-first");
        var secondMapping = UsageDeclaration("late-second", Document with { Subject = new SubjectId { Value = "dotnet://Library/Models.OtherCustomer" } });
        var lateUse = Usage("late-use");
        var otherMethod = Method with { Value = "dotnet://Application/Api.Other(System.String)" };
        _unrelated = Query with { Subject = new SubjectId { Value = "dotnet://Application/Api.Other" } };
        GenerationFact[] facts =
        [
            .. CompleteFacts(),
            firstMapping with { Subject = lateDeclaration, Definition = firstMapping.Definition with { Declaration = lateDeclaration } },
            secondMapping with { Subject = lateDeclaration, Definition = secondMapping.Definition with { Declaration = lateDeclaration } },
            lateUse with { Definition = lateUse.Definition with { Declaration = lateDeclaration } },
            Artifact(secondMapping.Definition.Artifact, "other-model", "OtherCustomer"),
            Artifact(_unrelated, "unrelated", "OtherLookup"),
            Entry("unrelated-entry", otherMethod, _unrelated),
            Usage("unrelated-use", method: otherMethod)
        ];
        _result = Derive(facts);
        _reversed = Derive([.. facts.Reverse()]);
    }

    [Fact] void should_not_publish_a_read_from_the_conflicted_entry_point() => _result.Facts.Any(record => ((RelationshipFact)record.Fact).Definition.Key.Source == Query.Subject).ShouldBeFalse();
    [Fact] void should_keep_an_unrelated_entry_point() => ((RelationshipFact)_result.Facts.Single().Fact).Definition.Key.Source.ShouldEqual(_unrelated.Subject);
    [Fact] void should_report_the_typed_mapping_conflict() => _result.Diagnostics.Single().Code.ShouldEqual(GenerationDiagnosticCodes.ConflictingArtifactUsageMapping);
    [Fact] void should_link_the_suppressed_earlier_read_proof() => _result.Diagnostics.Single().Facts.ShouldContain(new FactId { Value = "library:usage" });
    [Fact] void should_link_both_contradictory_declarations() => _result.Diagnostics.Single().Facts.Count(id => id.Value is "library:late-first" or "library:late-second").ShouldEqual(2);
    [Fact] void should_be_independent_of_input_order() => Projection(_reversed).ShouldEqual(Projection(_result));
}

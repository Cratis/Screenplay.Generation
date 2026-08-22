// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Generation.DotNet.Vogen.for_VogenConceptScreenplayAdapter;

public class when_projects_are_supplied_in_different_orders : given.a_vogen_compilation
{
    AdapterContribution _first = null!;
    string _firstJson = null!;
    string _secondJson = null!;

    void Because()
    {
        var firstProject = Project(
            "Project.A",
            CompilationFrom(
                "Shared",
                new SourceFile(
                    "/workspace/A/Code.cs",
                    """
                    namespace Shared;
                    [Vogen.ValueObject<System.Guid>]
                    public partial struct Code;
                    """)));
        var secondProject = Project(
            "Project.B",
            CompilationFrom(
                "Shared",
                new SourceFile(
                    "/workspace/B/Code.cs",
                    """
                    namespace Shared;
                    [Vogen.ValueObject<System.Guid>]
                    public partial struct Code;
                    """)));

        _first = Analyze(firstProject, secondProject);
        var second = Analyze(secondProject, firstProject);
        _firstJson = JsonSerializer.Serialize(_first);
        _secondJson = JsonSerializer.Serialize(second);
    }

    [Fact] void should_be_byte_deterministic() => _firstJson.ShouldEqual(_secondJson);
    [Fact] void should_keep_same_named_types_as_distinct_subjects() => _first.Facts.OfType<ArtifactFact>().Select(_ => _.Subject.Value).ShouldContainOnly("dotnet://Project.A/Shared/Shared.Code", "dotnet://Project.B/Shared/Shared.Code");
    [Fact] void should_assign_unique_deterministic_fact_ids() => _first.Facts.Select(_ => _.Id.Value).Distinct(StringComparer.Ordinal).Count().ShouldEqual(4);
    [Fact] void should_keep_exact_authored_evidence_for_both_subjects() => _first.Facts.All(_ => _.Evidence.Strength == EvidenceStrength.Exact && _.Evidence.Source!.StartLine == 2).ShouldBeTrue();
}

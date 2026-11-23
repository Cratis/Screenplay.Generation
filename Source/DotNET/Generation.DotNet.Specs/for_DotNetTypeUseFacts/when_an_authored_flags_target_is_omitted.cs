// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetTypeUseFacts;

public class when_an_authored_flags_target_is_omitted : given.a_compilation
{
    GeneratedScreenplayDefinition _result = null!;
    GeneratedScreenplayDefinition _withoutOmission = null!;
    SubjectId _flagsSubject = null!;
    ArtifactMemberTypeUseFact _typeUse = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile("/workspace/Ordering/SetPermissions.cs", """
            namespace Ordering;
            [System.Flags] public enum Permissions { None = 0, Read = 1, Write = 2 }
            public sealed record SetPermissions(Permissions Permissions);
            """));
        var project = new DotNetProjectCompilation { Name = "Ordering", Compilation = compilation, AuthoredSyntaxTrees = compilation.SyntaxTrees.ToHashSet() };
        var command = TypeNamed(compilation, "Ordering.SetPermissions");
        _flagsSubject = project.SubjectForType(TypeNamed(compilation, "Ordering.Permissions"));
        var adapter = new AdapterIdentity { Id = "example", Version = "1.0.0" };
        var evidence = new Evidence
        {
            Adapter = adapter, Strength = EvidenceStrength.Exact,
            Source = new SourceRange { Path = "SetPermissions.cs", StartLine = 3, StartColumn = 1, EndLine = 3, EndColumn = 60 }
        };
        var artifact = new ArtifactKey { Subject = project.SubjectForType(command), Kind = ArtifactKind.Command };
        var emitted = DotNetTypeUseFacts.Emit(command, artifact, new DotNetAnalysisContext([project]), evidence);
        var observed = emitted.OfType<ArtifactMemberTypeUseFact>().Single();

        // The adapter chooses an underlying-primitive fallback without discarding the exact observation.
        _typeUse = observed with { Definition = observed.Definition with { Type = observed.Definition.Type with { Name = "Int" } } };
        GenerationFact[] facts =
        [
            new ArtifactDeclarationFact
            {
                Id = new FactId { Value = "example:command" }, Subject = artifact.Subject, Evidence = evidence,
                Definition = new ArtifactDeclarationDefinition { Artifact = artifact, Name = "SetPermissions" }
            },
            new ArtifactPlacementFact
            {
                Id = new FactId { Value = "example:placement" }, Subject = artifact.Subject, Evidence = evidence, Artifact = artifact,
                Placement = new ArtifactPlacement { Module = "Ordering", Features = ["Access"], Slice = "Setting", SliceKind = GenerationSliceKind.StateChange }
            },
            .. emitted.Select(fact => fact == observed ? _typeUse : fact)
        ];
        var omission = new TypeUseTargetOmittedFact
        {
            Id = new FactId { Value = "example:omission" }, Subject = artifact.Subject, Evidence = evidence,
            Definition = new TypeUseTargetOmittedDefinition
            {
                Member = observed.Definition.Member,
                Diagnostic = new GenerationDiagnostic { Code = "example.FlagsEnumOmitted", Severity = GenerationDiagnosticSeverity.Warning, Message = "Flags enum omitted", Source = evidence.Source, Facts = [observed.Id] }
            }
        };
        var generator = new ScreenplayDefinitionGenerator();
        var options = new ScreenplayGenerationOptions { Domain = "Ordering" };
        _result = generator.Generate([new AdapterContribution { Adapter = adapter, Facts = [.. facts, omission] }], options);
        _withoutOmission = generator.Generate([new AdapterContribution { Adapter = adapter, Facts = facts }], options);
    }

    [Fact] void should_generate_a_compiler_verified_command() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_keep_the_member_with_its_underlying_primitive_fallback() => _result.Source.ShouldContain("permissions Int");
    [Fact] void should_leave_the_member_without_a_concept_subject() => _result.Graph.Artifacts.Single().Variants.Single().Definition.Properties.Single().Type.Subject.ShouldBeNull();
    [Fact] void should_retain_the_authored_flags_subject() => _typeUse.Definition.Type.ObservedTypeSubject.ShouldEqual(_flagsSubject);
    [Fact] void should_report_the_warning_exactly_once() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == "example.FlagsEnumOmitted").ShouldEqual(1);
    [Fact] void should_not_report_a_missing_target_or_incomplete_member() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.MissingTypeUseTarget || diagnostic.Code == GenerationDiagnosticCodes.IncompleteArtifactMember).ShouldBeFalse();
    [Fact] void should_still_report_a_missing_target_without_the_fact() => _withoutOmission.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.MissingTypeUseTarget).ShouldBeTrue();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_an_omitted_target_is_declared : given.an_omitted_type_use
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var facts = Facts();
        var target = DeclaredTarget(facts[0]);
        var representation = new ConceptRepresentationFact
        {
            Id = Id("representation"), Subject = Target, Evidence = target.Evidence,
            Definition = new ConceptRepresentationDefinition { Concept = Target, Kind = ConceptRepresentationKind.Primitive, Primitive = GenerationPrimitiveKind.WholeNumber }
        };
        _result = Generate([.. facts, target, representation]);
    }

    [Fact] void should_generate_successfully() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_bind_the_declared_target_instead() => _result.Graph.Artifacts.Single(artifact => artifact.Key.Kind == ArtifactKind.Command).Variants.Single().Definition.Properties.Single().Type.Subject.ShouldEqual(Target);
    [Fact] void should_not_report_the_omission_warning() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == "example.FlagsEnumOmitted").ShouldBeFalse();
    [Fact] void should_derive_the_binding() => _result.AdapterRun.Derivation.Facts.Any(record => record.Fact is TypeUseBindingFact).ShouldBeTrue();
}

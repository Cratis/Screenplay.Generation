// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_an_omission_belongs_to_another_producer_or_scope : given.an_omitted_type_use
{
    GeneratedScreenplayDefinition _otherProducer = null!;
    GeneratedScreenplayDefinition _otherScope = null!;

    void Because()
    {
        var facts = Facts();
        var omission = facts.OfType<TypeUseTargetOmittedFact>().Single();
        var scope = new BackendScopeKey { Store = new SubjectId { Value = "dotnet://Ordering/OtherStore" } };
        _otherProducer = Generate(facts.Select(fact => fact == omission
            ? omission with { Evidence = omission.Evidence with { Adapter = new AdapterIdentity { Id = "another", Version = "1.0.0" } } }
            : fact));
        _otherScope = Generate(facts.Select(fact => fact == omission
            ? omission with { Definition = omission.Definition with { Member = omission.Definition.Member with { Artifact = omission.Definition.Member.Artifact with { Scope = scope } } } }
            : fact));
    }

    [Fact] void should_not_waive_another_producers_type_use() => _otherProducer.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.MissingTypeUseTarget).ShouldBeTrue();
    [Fact] void should_not_waive_another_scopes_type_use() => _otherScope.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.MissingTypeUseTarget).ShouldBeTrue();
}

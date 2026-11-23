// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_an_omission_identifies_a_different_member : given.an_omitted_type_use
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var facts = Facts();
        var omission = facts.OfType<TypeUseTargetOmittedFact>().Single();
        _result = Generate(facts.Select(fact => fact == omission
            ? omission with { Definition = omission.Definition with { Member = omission.Definition.Member with { Name = "anotherMember" } } }
            : fact));
    }

    [Fact] void should_not_hide_the_missing_target() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.MissingTypeUseTarget).ShouldBeTrue();
    [Fact] void should_not_claim_the_omission_applies() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == "example.FlagsEnumOmitted").ShouldBeFalse();
}

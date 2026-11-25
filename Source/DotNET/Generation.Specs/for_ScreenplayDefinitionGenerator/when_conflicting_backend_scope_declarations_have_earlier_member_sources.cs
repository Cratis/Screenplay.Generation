// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_conflicting_backend_scope_declarations_have_earlier_member_sources : given.backend_scope_diagnostic_sources
{
    GeneratedScreenplayDefinition[] _results;

    void Because()
    {
        var conflicting = Declaration(First, "conflicting-scope", 6);
        conflicting = conflicting with { Definition = conflicting.Definition with { Name = "Another store" } };
        _results = GenerateOrders([conflicting, Declaration(First, "scope")], Members(First));
    }

    [Fact] void should_locate_the_conflict_at_the_canonical_declaration_source() => _results.All(result => result.Diagnostics.Single(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.ConflictingBackendScope).Source == At(5).Source).ShouldBeTrue();
    [Fact] void should_locate_the_unsupported_scope_warning_at_the_same_declaration() => _results.All(result => ScopeWarnings(result).Single().Source == At(5).Source).ShouldBeTrue();
    [Fact] void should_keep_diagnostics_identical_in_both_orders() => Describe(_results[1].Diagnostics).ShouldEqual(Describe(_results[0].Diagnostics));
}

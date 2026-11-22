// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_an_unscoped_use_has_only_scoped_targets : given.scoped_type_uses
{
    GenerationDerivationSnapshot _result;

    void Because() => _result = Derive(Facts(null));

    [Fact] void should_not_fan_out_or_select_an_owner_scope() => _result.Facts.ShouldBeEmpty();
    [Fact] void should_report_ambiguous_unqualified_ownership() => _result.Diagnostics.Single().Code.ShouldEqual(GenerationDiagnosticCodes.AmbiguousBackendScopeOwnership);
    [Fact] void should_link_both_legitimate_occurrences_without_selecting_either() => _result.Diagnostics.Single().Facts.Intersect([Id("first-model"), Id("second-model")]).Count().ShouldEqual(2);
}

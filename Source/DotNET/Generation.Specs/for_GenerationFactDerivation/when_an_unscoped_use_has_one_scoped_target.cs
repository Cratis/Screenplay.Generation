// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_an_unscoped_use_has_one_scoped_target : given.scoped_type_uses
{
    GenerationDerivationSnapshot _result;

    void Because() => _result = Derive(Facts(null).Where(fact => fact.Id != Id("first-model")));

    [Fact] void should_not_guess_the_only_store() => _result.Facts.ShouldBeEmpty();
    [Fact] void should_report_unknown_rather_than_ambiguous_qualification() => _result.Diagnostics.Single().Code.ShouldEqual(GenerationDiagnosticCodes.UnknownBackendScope);
}

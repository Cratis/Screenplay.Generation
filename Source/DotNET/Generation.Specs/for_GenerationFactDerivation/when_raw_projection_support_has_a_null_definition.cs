// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_raw_projection_support_has_a_null_definition : given.diagnostic_projection
{
    GenerationDerivationSnapshot _result;
    void Because() => _result = Derive([.. Inputs(), Support() with { Id = new() { Value = "observations:malformed-support" }, Definition = null! }]);
    [Fact] void should_ignore_the_malformed_support_without_throwing() => Projected(_result).Subject.ShouldEqual(CommandSubject);
    [Fact] void should_not_let_the_malformed_support_invalidate_the_valid_projection() => _result.Rules.SelectMany(rule => rule.Diagnostics).Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.InvalidDiagnosticIntent).ShouldBeFalse();
}

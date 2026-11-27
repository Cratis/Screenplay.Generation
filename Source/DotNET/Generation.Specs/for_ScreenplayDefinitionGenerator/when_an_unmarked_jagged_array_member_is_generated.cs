// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_an_unmarked_jagged_array_member_is_generated : given.jagged_array_members
{
    GeneratedScreenplayDefinition[] _results = null!;

    void Because() => _results = [Generate(false, null), Generate(false, ArrayOfArrays)];

    [Fact] void should_still_report_the_unsupported_shape_with_or_without_evidence() => _results.All(result => HasCode(result, GenerationDiagnosticCodes.UnsupportedTypeUseShape)).ShouldBeTrue();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_SpecificationFacts;

public class when_reading_discriminators : Specification
{
    [Fact] void should_reserve_negative_one_for_unknown_step_phases() => ((int)SpecificationStepPhase.Unknown).ShouldEqual(-1);
    [Fact] void should_reserve_negative_one_for_unknown_step_kinds() => ((int)SpecificationStepKind.Unknown).ShouldEqual(-1);
    [Fact] void should_reserve_negative_one_for_unknown_value_kinds() => ((int)SpecificationValueKind.Unknown).ShouldEqual(-1);
    [Fact] void should_keep_step_phase_values_stable() => Enum.GetValues<SpecificationStepPhase>().Cast<int>().ShouldContainOnly([-1, 0, 1, 2]);
    [Fact] void should_keep_step_kind_values_stable() => Enum.GetValues<SpecificationStepKind>().Cast<int>().ShouldContainOnly([-1, 0, 1, 2, 3, 4]);
    [Fact] void should_keep_value_kind_values_stable() => Enum.GetValues<SpecificationValueKind>().Cast<int>().ShouldContainOnly([-1, 0, 1, 2, 3, 4, 5]);
}

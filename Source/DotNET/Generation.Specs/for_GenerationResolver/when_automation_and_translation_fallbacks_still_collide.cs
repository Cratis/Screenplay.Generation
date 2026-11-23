// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_automation_and_translation_fallbacks_still_collide : given.mixed_slice_placements
{
    void Because() => graph = Resolve(Member("Automate", GenerationSliceKind.Automation, "Common"), Member("Translate", GenerationSliceKind.Translate, "Common"));

    [Fact] void should_use_the_known_kind_tokens() => Names(graph).ShouldEqual(["CommonAutomation", "CommonTranslate"]);
    [Fact] void should_not_leave_a_mixed_kind_conflict() => graph.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.ConflictingMixedSliceSplit).ShouldBeFalse();
}

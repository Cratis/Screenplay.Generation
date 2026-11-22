// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_EntryPointInputMemberDerivation;

public class when_an_execution_is_failure_only : given.input_members
{
    GenerationDerivationSnapshot _result;

    void Because() => _result = Derive(InputFacts(Support(), Classification(MethodExecutionKind.FailureOnly)));

    [Fact] void should_not_transfer_from_ineligible_observations() => Members(_result).ShouldBeEmpty();
    [Fact] void should_retain_the_frontends_original_warning() => _result.Diagnostics.Single().ShouldEqual(Reason());
}

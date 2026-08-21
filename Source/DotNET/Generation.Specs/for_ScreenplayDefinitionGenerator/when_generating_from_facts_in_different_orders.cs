// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_generating_from_facts_in_different_orders : given.a_generator
{
    GeneratedScreenplayDefinition _first = null!;
    GeneratedScreenplayDefinition _second = null!;

    void Because()
    {
        var opened = Event("AccountOpened", "Open", Property("accountId", "Uuid"));
        var deposited = Event("FundsDeposited", "Deposit", Property("amount", "Decimal"));
        var options = new ScreenplayGenerationOptions { Domain = "Banking" };

        _first = Generator.Generate([Contribution([.. opened, .. deposited])], options);
        _second = Generator.Generate([Contribution([.. deposited, .. opened])], options);
    }

    [Fact] void should_succeed_in_the_first_order() => _first.IsSuccess.ShouldBeTrue();
    [Fact] void should_succeed_in_the_second_order() => _second.IsSuccess.ShouldBeTrue();
    [Fact] void should_generate_identical_source() => _second.Source.ShouldEqual(_first.Source);
    [Fact] void should_include_the_domain() => _first.Source.ShouldContain("domain Banking");
    [Fact] void should_include_the_opened_event() => _first.Source.ShouldContain("event AccountOpened");
    [Fact] void should_include_the_deposited_event() => _first.Source.ShouldContain("event FundsDeposited");
    [Fact] void should_keep_repository_relative_file_references() => _first.Source.ShouldContain("file Accounts/Open/AccountOpened.cs");
}

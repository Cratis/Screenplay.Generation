// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_EntryPointInputMemberDerivation;

public class when_transferring_an_omitted_member_to_multiple_endpoints : given.omitted_input_members
{
    GenerationDerivationSnapshot _result;
    ArtifactKey _first;
    ArtifactKey _second;
    TypeUseTargetOmittedFact[] _omissions;

    void Because()
    {
        _first = Transfer().Definition.Artifact;
        _second = _first with { Subject = new SubjectId { Value = "dotnet://Application/Api.FindAnother" } };
        var method = new SubjectId { Value = "dotnet://Application/Api.FindAnother(System.String)" };
        var transfer = Transfer() with
        {
            Id = new FactId { Value = "second-input-transfer" },
            Subject = method,
            Definition = Transfer().Definition with { Method = method, Artifact = _second }
        };
        var execution = Execution() with
        {
            Id = new FactId { Value = "second-execution" },
            Subject = method,
            Definition = Execution().Definition with { Method = method }
        };
        _result = Derive([.. OmittedFacts(), Artifact(_second, "second-query", "FindAnotherCustomer"), Entry("second-entry", method, _second), execution, transfer]);
        _omissions = [.. _result.Facts.Select(record => record.Fact).OfType<TypeUseTargetOmittedFact>()];
    }

    [Fact] void should_report_the_shared_omission_warning_exactly_once() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == Omission().Definition.Diagnostic.Code).ShouldEqual(1);
    [Fact] void should_transfer_the_omission_to_the_first_endpoint() => _omissions.Count(omission => omission.Definition.Member.Artifact == _first).ShouldEqual(1);
    [Fact] void should_transfer_the_omission_to_the_second_endpoint() => _omissions.Count(omission => omission.Definition.Member.Artifact == _second).ShouldEqual(1);
    [Fact] void should_only_produce_the_two_endpoint_omissions() => _omissions.Length.ShouldEqual(2);
    [Fact] void should_not_report_a_missing_type_use_target() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.MissingTypeUseTarget).ShouldBeFalse();
}

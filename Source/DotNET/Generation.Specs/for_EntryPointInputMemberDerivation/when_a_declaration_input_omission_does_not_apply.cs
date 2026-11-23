// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_EntryPointInputMemberDerivation;

public class when_a_declaration_input_omission_does_not_apply : given.omitted_input_members
{
    GenerationDerivationSnapshot _missing;
    GenerationDerivationSnapshot _foreign;
    GenerationDerivationSnapshot _scoped;
    GenerationDerivationSnapshot _malformed;
    GenerationDerivationSnapshot _declared;

    void Because()
    {
        _missing = Derive([.. OmittedFacts().Where(fact => fact is not DeclarationInputMemberTargetOmittedFact)]);
        _foreign = Derive(ReplaceOmission(Omission() with { Evidence = UsageEvidence(ApplicationAdapter) }));
        _scoped = Derive(ReplaceOmission(Omission() with { Definition = Omission().Definition with { Member = Member() with { Scope = new BackendScopeKey { Store = FlagSubject } } } }));
        _malformed = Derive(ReplaceOmission(Omission() with { Definition = Omission().Definition with { Diagnostic = Omission().Definition.Diagnostic with { Code = string.Empty } } }));
        _declared = Derive([.. OmittedFacts(), Artifact(Document with { Subject = FlagSubject }, "flags-target", "Permissions")]);
    }

    [Fact] void should_keep_missing_targets_as_errors_without_an_omission() => HasMissingTarget(_missing).ShouldBeTrue();
    [Fact] void should_not_waive_another_producers_type_use() => HasMissingTarget(_foreign).ShouldBeTrue();
    [Fact] void should_not_waive_another_backend_scope() => HasMissingTarget(_scoped).ShouldBeTrue();
    [Fact] void should_not_waive_missing_targets_with_malformed_warnings() => HasMissingTarget(_malformed).ShouldBeTrue();
    [Fact] void should_diagnose_malformed_raw_omissions() => _malformed.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.InvalidDeclarationInputMemberTargetOmission).ShouldBeTrue();
    [Fact] void should_not_transfer_a_failed_member() => Members(_foreign).ShouldBeEmpty();
    [Fact] void should_prefer_an_exact_declared_target() => _declared.Facts.Select(record => record.Fact).OfType<TypeUseBindingFact>().Single().Definition.Target.Subject.ShouldEqual(FlagSubject);
    [Fact] void should_not_report_the_omission_warning_for_a_declared_target() => _declared.Diagnostics.ShouldBeEmpty();
    [Fact] void should_not_transfer_an_omission_for_a_declared_target() => _declared.Facts.Select(record => record.Fact).OfType<TypeUseTargetOmittedFact>().ShouldBeEmpty();

    static GenerationFact[] ReplaceOmission(DeclarationInputMemberTargetOmittedFact omission) => [.. OmittedFacts().Where(fact => fact is not DeclarationInputMemberTargetOmittedFact), omission];
    static bool HasMissingTarget(GenerationDerivationSnapshot result) => result.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.MissingTypeUseTarget);
}

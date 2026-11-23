// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_rejecting_invalid_declaration_input_target_omissions : given.declaration_input_target_omissions
{
    [Fact] void should_reject_another_producer() => Rejected(ObservedType() with { Evidence = Evidence() with { Adapter = new AdapterIdentity { Id = "other", Version = "1.0.0" } } }, Omission());
    [Fact] void should_reject_another_member() => Rejected(ObservedType(), Omission() with { Definition = Omission().Definition with { Member = Surface().Definition.Member with { Name = "other" } } });
    [Fact] void should_reject_another_declaration() => Rejected(ObservedType(), Omission() with { Definition = Omission().Definition with { Member = Surface().Definition.Member with { Declaration = ExternalSubject } } });
    [Fact] void should_reject_another_scope() => Rejected(ObservedType(), Omission() with { Definition = Omission().Definition with { Member = Surface().Definition.Member with { Scope = new BackendScopeKey { Store = ExternalSubject } } } });
    [Fact] void should_require_an_observed_subject() => Rejected(SurfaceType(), Omission());
    [Fact] void should_reject_duplicate_omissions() => AdmitOmission(ObservedType(), Omission(), Omission() with { Id = Id("second") }).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_unknown_severity() => InvalidDiagnostic(Omission().Definition.Diagnostic with { Severity = GenerationDiagnosticSeverity.Unknown });
    [Fact] void should_reject_non_warning_severity() => InvalidDiagnostic(Omission().Definition.Diagnostic with { Severity = GenerationDiagnosticSeverity.Error });
    [Fact] void should_require_a_code() => InvalidDiagnostic(Omission().Definition.Diagnostic with { Code = string.Empty });
    [Fact] void should_require_a_message() => InvalidDiagnostic(Omission().Definition.Diagnostic with { Message = string.Empty });
    [Fact] void should_validate_references() => InvalidDiagnostic(Omission().Definition.Diagnostic with { Facts = [Id("missing")] });
    [Fact] void should_reject_another_warning_subject() => InvalidDiagnostic(Omission().Definition.Diagnostic with { Subject = ExternalSubject });
    [Fact] void should_reject_another_warning_source() => InvalidDiagnostic(Omission().Definition.Diagnostic with { Source = Source() with { Path = "Other.cs" } });
    [Fact] void should_require_a_diagnostic() => Rejected(ObservedType(), Omission() with { Definition = Omission().Definition with { Diagnostic = null! } });

    static void InvalidDiagnostic(GenerationDiagnostic diagnostic) => Rejected(ObservedType(), Omission(diagnostic));
    static void Rejected(DeclarationInputMemberTypeUseFact typeUse, DeclarationInputMemberTargetOmittedFact omission) => AdmitOmission(typeUse, omission).IsAdmitted.ShouldBeFalse();
}

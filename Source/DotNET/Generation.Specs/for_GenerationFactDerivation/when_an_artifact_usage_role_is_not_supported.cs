// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_an_artifact_usage_role_is_not_supported : given.an_artifact_usage
{
    GenerationDerivationSnapshot _result;

    void Because() => _result = Derive([.. CompleteFacts().Where(fact => fact is not MethodArtifactUsageFact), Usage(kind: RelationshipKind.Stores)]);

    [Fact] void should_not_coerce_another_role_to_reads() => _result.Facts.ShouldBeEmpty();
    [Fact] void should_report_unsupported_semantics() => _result.Diagnostics.Single().Code.ShouldEqual(GenerationDiagnosticCodes.UnsupportedArtifactUsageRole);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_EntryPointInputMemberDerivation;

public class when_input_only_and_artifact_executions_compete : given.input_only_execution
{
    GenerationDerivationSnapshot _ordered;
    GenerationDerivationSnapshot _missing;
    GenerationDerivationSnapshot _tied;

    void Because()
    {
        var earlier = new SubjectId { Value = "declaration:earlier" };
        var execution = Execution() with { Id = new FactId { Value = "zz-earliest" }, Definition = Execution().Definition with { Declaration = earlier, ExecutionOrder = 0 } };
        var facts = InputOnlyFacts(Surface(declaration: earlier), SurfaceType(type: new TypeUseDefinition { Name = "int", Shape = [TypeUseShapeKind.Named] }, declaration: earlier));
        facts = [.. facts.Where(fact => fact is not MethodDeclarationExecutionFact), InputExecution(4)];
        _ordered = Derive([.. facts, execution]);
        _missing = Derive([.. facts, execution with { Definition = execution.Definition with { ExecutionOrder = null } }]);
        _tied = Derive([.. facts, execution with { Definition = execution.Definition with { ExecutionOrder = 4 } }]);
    }

    [Fact] void should_use_semantic_order_across_both_execution_families() => _ordered.Facts.Select(record => record.Fact).OfType<ArtifactMemberTypeUseFact>().Single().Definition.Type.Name.ShouldEqual("int");
    [Fact] void should_transfer_the_name_once() => Members(_ordered).Length.ShouldEqual(1);
    [Fact] void should_not_use_lexical_order_for_missing_positions() => Members(_missing).ShouldBeEmpty();
    [Fact] void should_not_use_lexical_order_for_tied_positions() => Members(_tied).ShouldBeEmpty();
    [Fact] void should_diagnose_ambiguous_precedence() => new[] { _missing, _tied }.All(result => result.Diagnostics.Any(diagnostic => diagnostic.Outcome == GenerationDiagnosticOutcome.Conflict)).ShouldBeTrue();
}

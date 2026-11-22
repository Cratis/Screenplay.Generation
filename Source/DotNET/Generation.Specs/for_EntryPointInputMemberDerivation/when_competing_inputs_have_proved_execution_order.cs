// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_EntryPointInputMemberDerivation;

public class when_competing_inputs_have_proved_execution_order : given.input_members
{
    GenerationDerivationSnapshot _result;

    void Because()
    {
        var other = new SubjectId { Value = "project:earlier-declaration" };
        var type = new TypeUseDefinition { Name = "int", Shape = [TypeUseShapeKind.Named] };
        var usage = Execution() with { Id = new FactId { Value = "zz-earlier-execution" }, Definition = Execution().Definition with { Declaration = other, ExecutionOrder = 0 } };
        _result = Derive([.. InputFacts(Surface(declaration: other), SurfaceType(type: type, declaration: other), usage).Where(fact => fact is not MethodArtifactUsageFact || fact.Id == usage.Id), Execution(4)]);
    }

    [Fact] void should_transfer_the_earliest_execution_not_the_lexical_fact_id() => _result.Facts.Select(record => record.Fact).OfType<ArtifactMemberTypeUseFact>().Single().Definition.Type.Name.ShouldEqual("int");
    [Fact] void should_transfer_a_name_once() => Members(_result).Count().ShouldEqual(1);
}

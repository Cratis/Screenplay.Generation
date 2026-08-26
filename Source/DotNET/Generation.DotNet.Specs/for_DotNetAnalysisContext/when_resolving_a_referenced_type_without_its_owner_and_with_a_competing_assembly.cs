// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetAnalysisContext;

public class when_resolving_a_referenced_type_without_its_owner_and_with_a_competing_assembly : given.project_reference_compilations
{
    SubjectId? _subject;

    void Because()
    {
        var context = ProjectReferenceWithCompetingAssemblyAt("/workspace");

        _subject = new DotNetAnalysisContext([context.Competing, context.Root]).SubjectForType(context.ReferencedType);
    }

    [Fact] void should_not_treat_the_competing_project_as_the_owner() => _subject.ShouldBeNull();
}

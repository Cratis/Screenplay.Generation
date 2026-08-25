// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetAnalysisContext;

public class when_resolving_a_referenced_project_type_from_both_projects : given.project_reference_compilations
{
    SubjectId _expected = null!;
    SubjectId? _ownerFirst;
    SubjectId? _rootFirst;

    void Because()
    {
        var context = ProjectReferenceAt("/workspace");
        _expected = context.Owner.SubjectForType(context.OwnerType);
        _ownerFirst = new DotNetAnalysisContext([context.Owner, context.Root]).SubjectForType(context.ReferencedType);
        _rootFirst = new DotNetAnalysisContext([context.Root, context.Owner]).SubjectForType(context.ReferencedType);
    }

    [Fact] void should_resolve_the_exact_owner_qualified_subject_with_the_owner_first() => _ownerFirst.ShouldEqual(_expected);
    [Fact] void should_resolve_the_exact_owner_qualified_subject_with_the_root_first() => _rootFirst.ShouldEqual(_expected);
}

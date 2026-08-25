// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetAnalysisContext;

public class when_resolving_a_referenced_project_type_from_relocated_checkouts : given.project_reference_compilations
{
    SubjectId? _first;
    SubjectId? _second;
    SubjectId _expected = null!;

    void Because()
    {
        var first = ProjectReferenceAt("/first-checkout");
        var second = ProjectReferenceAt("/relocated/second-checkout");
        _expected = first.Owner.SubjectForType(first.OwnerType);
        _first = new DotNetAnalysisContext([first.Root, first.Owner]).SubjectForType(first.ReferencedType);
        _second = new DotNetAnalysisContext([second.Owner, second.Root]).SubjectForType(second.ReferencedType);
    }

    [Fact] void should_resolve_the_exact_owner_qualified_subject_before_relocation() => _first.ShouldEqual(_expected);
    [Fact] void should_resolve_the_same_owner_qualified_subject_after_relocation() => _second.ShouldEqual(_expected);
}

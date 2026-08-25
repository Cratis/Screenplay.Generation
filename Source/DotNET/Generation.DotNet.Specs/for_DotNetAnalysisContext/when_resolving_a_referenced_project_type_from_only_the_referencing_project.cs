// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetAnalysisContext;

public class when_resolving_a_referenced_project_type_from_only_the_referencing_project : given.project_reference_compilations
{
    SubjectId? _subject;
    ProjectReferenceContext _context = null!;

    void Because()
    {
        _context = ProjectReferenceAt("/workspace");
        _subject = new DotNetAnalysisContext([_context.Root]).SubjectForType(_context.ReferencedType);
    }

    [Fact] void should_use_a_real_csharp_compilation_reference() => _context.ProjectReference.GetType().FullName.ShouldEqual("Microsoft.CodeAnalysis.CSharp.CSharpCompilationReference");
    [Fact] void should_preserve_the_referenced_type_source_location() => _context.ReferencedType.Locations.Any(_ => _.IsInSource).ShouldBeTrue();
    [Fact] void should_not_treat_the_referencing_project_as_the_owner() => _subject.ShouldBeNull();
}

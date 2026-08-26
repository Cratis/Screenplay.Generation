// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetAnalysisContext;

public class when_resolving_a_referenced_type_with_a_competing_assembly : given.project_reference_compilations
{
    SubjectId _expected = null!;
    CompetingAssemblyContext _context = null!;
    SubjectId? _ownerFirst;
    SubjectId? _competingFirst;

    void Because()
    {
        _context = ProjectReferenceWithCompetingAssemblyAt("/workspace");
        _expected = _context.Owner.SubjectForType(_context.OwnerType);
        _ownerFirst = new DotNetAnalysisContext([_context.Owner, _context.Competing, _context.Root]).SubjectForType(_context.ReferencedType);
        _competingFirst = new DotNetAnalysisContext([_context.Competing, _context.Root, _context.Owner]).SubjectForType(_context.ReferencedType);
    }

    [Fact] void should_use_a_real_csharp_compilation_reference() => _context.ProjectReference.GetType().FullName.ShouldEqual("Microsoft.CodeAnalysis.CSharp.CSharpCompilationReference");
    [Fact] void should_give_both_candidates_the_same_simple_assembly_name() => _context.Competing.Compilation.Assembly.Identity.Name.ShouldEqual(_context.Owner.Compilation.Assembly.Identity.Name);
    [Fact] void should_give_the_candidates_different_exact_assembly_identities() => _context.Competing.Compilation.Assembly.Identity.Equals(_context.Owner.Compilation.Assembly.Identity).ShouldBeFalse();
    [Fact] void should_resolve_the_true_owner_with_the_owner_first() => _ownerFirst.ShouldEqual(_expected);
    [Fact] void should_resolve_the_true_owner_with_the_competing_project_first() => _competingFirst.ShouldEqual(_expected);
}

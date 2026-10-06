// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator.given;

public class a_verifying_generator : a_generator
{
    protected Diagnostic[] CompilerDiagnostics = [];
    protected GeneratedScreenplayDefinition ContributionsResult = null!;
    protected GeneratedScreenplayDefinition SnapshotResult = null!;

    protected void GenerateBoth()
    {
        var generator = new ScreenplayDefinitionGenerator(new GenerationResolver(), new ScreenplayLowerer(), new ScreenplayPrinter(), new VerifyingCompiler(CompilerDiagnostics));
        var facts = Event("AccountOpened", "Open");
        var options = new ScreenplayGenerationOptions { Domain = "Banking" };
        ContributionsResult = generator.Generate([Contribution(facts)], options);
        SnapshotResult = generator.Generate(Snapshot(Completed(Adapter, facts)), options);
    }

    sealed class VerifyingCompiler(Diagnostic[] diagnostics) : IScreenplayCompiler
    {
        readonly IScreenplayCompiler _compiler = new ScreenplayCompiler();

        public CompilationResult<ApplicationSyntax> Compile(string source) => new(_compiler.Compile(source).Value, diagnostics);

        public CompilationResult<TApplication> Compile<TApplication>(string source, IApplicationSyntaxVisitor<TApplication> visitor) => _compiler.Compile(source, visitor);

        public CompilationResult<ApplicationSyntax> Parse(string source, string? path = null) => _compiler.Parse(source, path);

        public CompilationResult<ProjectionSyntax> CompileProjection(string source) => _compiler.CompileProjection(source);

        public CompilationResult<TProjection> CompileProjection<TProjection>(string source, IProjectionSyntaxVisitor<TProjection> visitor) => _compiler.CompileProjection(source, visitor);

        public CompilationResult<SpecificationSyntax> CompileSpecification(string source) => _compiler.CompileSpecification(source);

        public CompilationResult<TSpecification> CompileSpecification<TSpecification>(string source, ISpecificationSyntaxVisitor<TSpecification> visitor) => _compiler.CompileSpecification(source, visitor);

        public CompilationResult<CaptureSyntax> CompileCapture(string source) => _compiler.CompileCapture(source);

        public CompilationResult<TCapture> CompileCapture<TCapture>(string source, ICaptureSyntaxVisitor<TCapture> visitor) => _compiler.CompileCapture(source, visitor);
    }
}

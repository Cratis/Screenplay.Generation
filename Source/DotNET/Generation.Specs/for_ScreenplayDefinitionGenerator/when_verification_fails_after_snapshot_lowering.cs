// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_verification_fails_after_snapshot_lowering : given.a_generator
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var generator = new ScreenplayDefinitionGenerator(
            new GenerationResolver(),
            new ScreenplayLowerer(),
            new ScreenplayPrinter(),
            new FailingCompiler());
        _result = generator.Generate(
            Snapshot(Completed(Adapter, Event("AccountOpened", "Open"))),
            new ScreenplayGenerationOptions { Domain = "Banking" });
    }

    [Fact] void should_report_the_verification_failure() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(GenerationDiagnosticCodes.DocumentDidNotCompile);
    [Fact] void should_mark_generation_as_unsuccessful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_keep_the_lowered_artifact_disposition() => Disposition("event:AccountOpened").ShouldEqual(GenerationFactDisposition.Lowered);
    [Fact] void should_keep_the_lowered_placement_disposition() => Disposition("placement:AccountOpened").ShouldEqual(GenerationFactDisposition.Lowered);

    GenerationFactDisposition Disposition(string id) => _result.AdapterRun!.Facts.Single(record => record.Fact.Id.Value == id).Disposition;

    sealed class FailingCompiler : IScreenplayCompiler
    {
        readonly IScreenplayCompiler _compiler = new ScreenplayCompiler();

        public CompilationResult<ApplicationSyntax> Compile(string source) => CompilationResult<ApplicationSyntax>.Failed(
        [
            Diagnostic.Error("PLAY-FAILED", "Verification failed", SourceLocation.Start)
        ]);

        public CompilationResult<TApplication> Compile<TApplication>(string source, IApplicationSyntaxVisitor<TApplication> visitor) =>
            _compiler.Compile(source, visitor);

        public CompilationResult<ApplicationSyntax> Parse(string source, string? path = null) =>
            _compiler.Parse(source, path);

        public CompilationResult<ProjectionSyntax> CompileProjection(string source) =>
            _compiler.CompileProjection(source);

        public CompilationResult<TProjection> CompileProjection<TProjection>(string source, IProjectionSyntaxVisitor<TProjection> visitor) =>
            _compiler.CompileProjection(source, visitor);

        public CompilationResult<SpecificationSyntax> CompileSpecification(string source) =>
            _compiler.CompileSpecification(source);

        public CompilationResult<TSpecification> CompileSpecification<TSpecification>(string source, ISpecificationSyntaxVisitor<TSpecification> visitor) =>
            _compiler.CompileSpecification(source, visitor);

        public CompilationResult<CaptureSyntax> CompileCapture(string source) =>
            _compiler.CompileCapture(source);

        public CompilationResult<TCapture> CompileCapture<TCapture>(string source, ICaptureSyntaxVisitor<TCapture> visitor) =>
            _compiler.CompileCapture(source, visitor);
    }
}

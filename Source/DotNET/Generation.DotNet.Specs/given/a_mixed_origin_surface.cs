// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.given;

public class a_mixed_origin_surface : a_compilation
{
    protected DotNetAnalysisContext Context;
    protected INamedTypeSymbol Type;
    protected ArtifactKey Artifact;
    protected Evidence Evidence;

    void Establish()
    {
        var compilation = CompilationFrom(
            new SourceFile("Authored.cs", """
                namespace Contracts;
                public record CustomerCode;
                public record Derived : NonAuthored
                {
                    public string Local { get; init; }
                    public override CustomerCode Value { get; }
                    public new int Hidden { get; init; }
                    public new string Readable { private get; set; }
                }
                """),
            new SourceFile("Other.cs", """
                namespace Contracts;
                public record NonAuthored : Generated<CustomerCode>
                {
                    public string Other { get; init; }
                }
                """),
            new SourceFile("Generated.g.cs", """
                namespace Contracts;
                public record Generated<T> : External.Base<T>
                {
                    public string GeneratedProperty { get; init; }
                }
                """))
            .AddReferences(MetadataBase());
        compilation.GetDiagnostics().Where(_ => _.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        Context = new DotNetAnalysisContext(
        [
            new DotNetProjectCompilation
            {
                Name = "Application",
                Compilation = compilation,
                AuthoredSyntaxTrees = compilation.SyntaxTrees.Where(_ => _.FilePath == "Authored.cs").ToHashSet()
            }
        ]);
        Type = TypeNamed(compilation, "Contracts.Derived");
        Artifact = new ArtifactKey { Subject = DotNetSubjectIds.ForType(Type, "Application"), Kind = ArtifactKind.ReadModel };
        Evidence = new Evidence { Adapter = new AdapterIdentity { Id = "fixture", Version = "1.0.0" }, Strength = EvidenceStrength.Exact };
    }

    protected IReadOnlyList<GenerationFact> FactsFor(DotNetMemberOriginPolicy policy) =>
        DotNetTypeUseFacts.EmitWithSurface(Type, Artifact, Context, Evidence, policy, DotNetMemberSurfaceScope.Inherited, property => property.Name == "Id" ? ArtifactMemberRoleKind.Identifier : null);

    protected IReadOnlyList<PropertyDefinition> PropertiesFor(DotNetMemberOriginPolicy policy) =>
        DotNetTypeShapes.PropertiesOf(Type, Context, policy, DotNetMemberSurfaceScope.Inherited);

    static MetadataReference MetadataBase()
    {
        var compilation = CompilationFrom(new SourceFile("External.cs", """
            namespace External;
            public record Code;
            public record Base<T>
            {
                public T Id { get; init; }
                public string Zed { get; init; }
                public string Alpha { get; init; }
                public string Hidden { get; init; }
                public string Readable { get; init; }
                public Code ExternalCode { get; init; }
                public virtual object Value { get; }
                public static string Static { get; }
                public string this[int index] => "";
            }
            """)).WithAssemblyName("External.Contracts");
        using var stream = new MemoryStream();
        compilation.Emit(stream).Success.ShouldBeTrue();
        return MetadataReference.CreateFromImage(stream.ToArray());
    }
}

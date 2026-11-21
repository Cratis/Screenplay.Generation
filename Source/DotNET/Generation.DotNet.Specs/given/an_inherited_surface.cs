// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.given;

public class an_inherited_surface : a_compilation
{
    protected INamedTypeSymbol Type;
    protected DotNetAnalysisContext Context;
    protected ArtifactKey Artifact;
    protected Evidence Evidence;
    protected SubjectId CodeSubject;

    void Establish()
    {
        var compilation = CompilationFrom(
            new SourceFile("/workspace/Surface.cs", """
                #nullable enable
                namespace Banking;
                public class CustomerCode;
                public class Base<T>
                {
                    public T[]? Codes { get; }
                    public virtual object Value { get; }
                    public string Hidden { get; }
                    public string Readable { get; }
                    public static string Static { get; }
                    public string this[int index] => "";
                }
                public partial class Derived : Base<CustomerCode>
                {
                    public string Local { get; }
                    public override CustomerCode Value { get; }
                    public new int Hidden { get; }
                    public new string Readable { private get; set; }
                }
                """),
            new SourceFile("/workspace/Partial.cs", """
                namespace Banking;
                public partial class Derived { public int Partial { get; } }
                """));
        var project = new DotNetProjectCompilation { Name = "Banking", Compilation = compilation, AuthoredSyntaxTrees = compilation.SyntaxTrees.ToHashSet() };
        Type = TypeNamed(compilation, "Banking.Derived");
        Context = new DotNetAnalysisContext([project]);
        Artifact = new ArtifactKey { Subject = project.SubjectForType(Type), Kind = ArtifactKind.Event };
        CodeSubject = project.SubjectForType(TypeNamed(compilation, "Banking.CustomerCode"));
        Evidence = new Evidence { Adapter = new AdapterIdentity { Id = "surface", Version = "1.0.0" }, Strength = EvidenceStrength.Exact };
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSource;

public class when_reading_authored_source : given.a_compilation
{
    IReadOnlyList<SyntaxReference> _declarations = null!;
    IReadOnlyList<AttributeData> _attributes = null!;
    IReadOnlyList<AttributeData> _assemblyAttributes = null!;
    Evidence _attributeEvidence = null!;
    INamedTypeSymbol _type = null!;

    void Because()
    {
        var compilation = CompilationFrom(
            new SourceFile(
                "/workspace/Banking/AccountId.cs",
                """
                [assembly: Banking.Marker]
                namespace Banking;
                [System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true)]
                public sealed class MarkerAttribute : System.Attribute;
                [Marker]
                public readonly partial record struct AccountId;
                """),
            new SourceFile(
                "/workspace/Banking/AccountId.g.cs",
                """
                namespace Banking;
                [Marker]
                public readonly partial record struct AccountId;
                """));
        _type = TypeNamed(compilation, "Banking.AccountId");
        _declarations = DotNetSource.AuthoredDeclarationsOf(_type);
        _attributes = DotNetSource.AuthoredAttributesOf(_type);
        _assemblyAttributes = DotNetSource.AuthoredAttributesOf(compilation.Assembly);
        _attributeEvidence = DotNetSource.EvidenceFor(
            _attributes.Single(),
            new AdapterIdentity { Id = "test", Version = "1.0.0" },
            EvidenceStrength.Exact,
            "/workspace");
    }

    [Fact] void should_only_return_the_authored_declaration() => _declarations.Select(_ => _.SyntaxTree.FilePath).ShouldContainOnly("/workspace/Banking/AccountId.cs");
    [Fact] void should_recognize_the_authored_partial_declaration() => DotNetSource.HasAuthoredPartialDeclaration(_type).ShouldBeTrue();
    [Fact] void should_only_return_the_authored_attribute_application() => _attributes.Count.ShouldEqual(1);
    [Fact] void should_return_authored_assembly_attributes() => _assemblyAttributes.Count.ShouldEqual(1);
    [Fact] void should_anchor_attribute_evidence_at_the_attribute_application() => _attributeEvidence.Source.ShouldEqual(new SourceRange { Path = "Banking/AccountId.cs", StartLine = 5, StartColumn = 2, EndLine = 5, EndColumn = 8 });
}

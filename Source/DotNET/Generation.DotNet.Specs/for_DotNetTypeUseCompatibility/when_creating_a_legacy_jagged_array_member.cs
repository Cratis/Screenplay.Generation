// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetTypeUseCompatibility;

public class when_creating_a_legacy_jagged_array_member : given.a_compilation
{
    IReadOnlyDictionary<string, ArtifactMemberTypeUseDefinition?> _members = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/Ordering/Shapes.cs",
            """
            #nullable enable
            namespace Ordering;
            public sealed record Shapes(
                int[][] Jagged,
                int[][]? OptionalJagged,
                int[] SingleArray,
                int[,] Matrix,
                int[,][] MatrixOfArrays,
                int[][,] ArrayOfMatrices,
                System.Collections.Generic.List<int[]> ListOfArrays,
                System.Collections.Generic.List<System.Collections.Generic.List<int>> ListOfLists,
                int Scalar);
            """));
        var project = new DotNetProjectCompilation
        {
            Name = "Ordering",
            Compilation = compilation,
            AuthoredSyntaxTrees = compilation.SyntaxTrees.ToHashSet()
        };
        var context = new DotNetAnalysisContext([project]);
        var artifact = new ArtifactKey { Subject = new SubjectId { Value = "dotnet://Ordering/Events.Shapes" }, Kind = ArtifactKind.Event };
        _members = TypeNamed(compilation, "Ordering.Shapes").GetMembers()
            .OfType<IPropertySymbol>()
            .Where(property => !property.IsStatic && property.DeclaredAccessibility == Accessibility.Public)
            .ToDictionary(
                property => property.Name,
                property => DotNetTypeUseCompatibility.TryCreateLegacyJaggedArrayMember(property.Type, context, new ArtifactMemberKey { Artifact = artifact, Name = property.Name }, out var definition) ? definition : null,
                StringComparer.Ordinal);
    }

    [Fact] void should_mark_an_array_of_arrays() => _members["Jagged"]!.UseLegacyJaggedArrayReference.ShouldBeTrue();
    [Fact] void should_read_array_origins_from_the_symbols() => Origins("Jagged").ShouldEqual("Array,Array");
    [Fact] void should_keep_the_exact_observed_shape() => string.Join('|', _members["Jagged"]!.Type.Shape).ShouldEqual("Collection|Collection|Named");
    [Fact] void should_mark_an_optional_outer_array() => _members["OptionalJagged"]!.Type.Shape[0].ShouldEqual(TypeUseShapeKind.Optional);
    [Fact] void should_not_mark_a_single_array() => _members["SingleArray"].ShouldBeNull();
    [Fact] void should_not_mark_a_multidimensional_array() => _members["Matrix"].ShouldBeNull();
    [Fact] void should_mark_an_array_of_multidimensional_arrays_like_the_legacy_reference() => Origins("ArrayOfMatrices").ShouldEqual("Array,Array");
    [Fact] void should_mark_a_multidimensional_array_of_arrays_like_the_legacy_reference() => Origins("MatrixOfArrays").ShouldEqual("Array,Array");
    [Fact] void should_not_mark_a_list_of_arrays() => _members["ListOfArrays"].ShouldBeNull();
    [Fact] void should_not_mark_a_list_of_lists() => _members["ListOfLists"].ShouldBeNull();
    [Fact] void should_not_mark_a_scalar() => _members["Scalar"].ShouldBeNull();

    string Origins(string name) => string.Join(',', _members[name]!.CollectionOrigins!);
}

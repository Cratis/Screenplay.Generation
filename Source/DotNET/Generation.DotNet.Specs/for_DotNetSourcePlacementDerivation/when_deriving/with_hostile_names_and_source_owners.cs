// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourcePlacementDerivation.when_deriving;

public class with_hostile_names_and_source_owners : Specification
{
    static readonly (string Label, string Value)[] _names =
    [
        ("plain", "Commerce"),
        ("slash", "Commerce/Orders"),
        ("backslash", "Commerce\\Orders"),
        ("dot", "."),
        ("dot-dot", ".."),
        ("three-dots", "..."),
        ("encoded-dot-dot", "%2e%2e"),
        ("leading-space", " Commerce"),
        ("trailing-space", "Commerce "),
        ("inner-space", "Com merce"),
        ("whitespace", "  "),
        ("empty", string.Empty),
        ("control-tab", "Com\tmerce"),
        ("control-nul", "Com\u0000merce"),
        ("drive", "C:"),
        ("unicode-precomposed", "Caf\u00e9"),
        ("unicode-decomposed", "Cafe\u0301"),
        ("unicode-letters", "\u65e5\u672c"),
        ("trailing-separator", "Commerce/")
    ];

    static readonly (string Label, string Value)[] _owners =
    [
        ("plain", "dotnet://Ordering/Ordering/Owner"),
        ("leading-space", " dotnet://Ordering/Ordering/Owner"),
        ("trailing-space", "dotnet://Ordering/Ordering/Owner "),
        ("control-tab", "dotnet://Ordering/Ordering/Ow\tner"),
        ("empty", string.Empty),
        ("whitespace", "   "),
        ("unicode", "dotnet://Ordering/Ordering/\u00c9quipe")
    ];

    string[] _moduleResults = null!;
    string[] _featureResults = null!;
    string[] _sliceResults = null!;
    string[] _ownerResults = null!;

    void Because()
    {
        _moduleResults = [.. _names.Select(_ => $"{_.Label}|{Describe(Derive(Placement() with { Module = _.Value }))}")];
        _featureResults = [.. _names.Select(_ => $"{_.Label}|{Describe(Derive(Placement() with { Features = [_.Value] }))}")];
        _sliceResults = [.. _names.Select(_ => $"{_.Label}|{Describe(Derive(Placement() with { Slice = _.Value }))}")];
        _ownerResults = [.. _owners.Select(_ => $"{_.Label}|{Describe(DeriveWithOwner(_.Value))}")];
    }

    [Fact]
    void should_judge_every_hostile_module_name_exactly() => string.Join('\n', _moduleResults).ShouldEqual(
        """
        plain|placement:Commerce/Orders/Place|compat=True
        slash|DOTNETSP0015:Unsupported
        backslash|DOTNETSP0015:Unsupported
        dot|DOTNETSP0015:Unsupported
        dot-dot|DOTNETSP0015:Unsupported
        three-dots|placement:.../Orders/Place|compat=True
        encoded-dot-dot|placement:%2e%2e/Orders/Place|compat=True
        leading-space|DOTNETSP0015:Unsupported
        trailing-space|DOTNETSP0015:Unsupported
        inner-space|placement:Com merce/Orders/Place|compat=True
        whitespace|DOTNETSP0015:Unsupported
        empty|DOTNETSP0015:Unsupported
        control-tab|DOTNETSP0015:Unsupported
        control-nul|DOTNETSP0015:Unsupported
        drive|placement:C:/Orders/Place|compat=True
        unicode-precomposed|placement:Caf\u00e9/Orders/Place|compat=True
        unicode-decomposed|placement:Cafe\u0301/Orders/Place|compat=True
        unicode-letters|placement:\u65e5\u672c/Orders/Place|compat=True
        trailing-separator|DOTNETSP0015:Unsupported
        """);

    [Fact]
    void should_judge_every_hostile_feature_name_exactly() => string.Join('\n', _featureResults).ShouldEqual(
        """
        plain|placement:Commerce/Commerce/Place|compat=True
        slash|DOTNETSP0015:Unsupported
        backslash|DOTNETSP0015:Unsupported
        dot|DOTNETSP0015:Unsupported
        dot-dot|DOTNETSP0015:Unsupported
        three-dots|placement:Commerce/.../Place|compat=True
        encoded-dot-dot|placement:Commerce/%2e%2e/Place|compat=True
        leading-space|DOTNETSP0015:Unsupported
        trailing-space|DOTNETSP0015:Unsupported
        inner-space|placement:Commerce/Com merce/Place|compat=True
        whitespace|DOTNETSP0015:Unsupported
        empty|DOTNETSP0015:Unsupported
        control-tab|DOTNETSP0015:Unsupported
        control-nul|DOTNETSP0015:Unsupported
        drive|placement:Commerce/C:/Place|compat=True
        unicode-precomposed|placement:Commerce/Caf\u00e9/Place|compat=True
        unicode-decomposed|placement:Commerce/Cafe\u0301/Place|compat=True
        unicode-letters|placement:Commerce/\u65e5\u672c/Place|compat=True
        trailing-separator|DOTNETSP0015:Unsupported
        """);

    [Fact]
    void should_judge_every_hostile_slice_name_exactly() => string.Join('\n', _sliceResults).ShouldEqual(
        """
        plain|placement:Commerce/Orders/Commerce|compat=True
        slash|DOTNETSP0015:Unsupported
        backslash|DOTNETSP0015:Unsupported
        dot|DOTNETSP0015:Unsupported
        dot-dot|DOTNETSP0015:Unsupported
        three-dots|placement:Commerce/Orders/...|compat=True
        encoded-dot-dot|placement:Commerce/Orders/%2e%2e|compat=True
        leading-space|DOTNETSP0015:Unsupported
        trailing-space|DOTNETSP0015:Unsupported
        inner-space|placement:Commerce/Orders/Com merce|compat=True
        whitespace|DOTNETSP0015:Unsupported
        empty|DOTNETSP0015:Unsupported
        control-tab|DOTNETSP0015:Unsupported
        control-nul|DOTNETSP0015:Unsupported
        drive|placement:Commerce/Orders/C:|compat=True
        unicode-precomposed|placement:Commerce/Orders/Caf\u00e9|compat=True
        unicode-decomposed|placement:Commerce/Orders/Cafe\u0301|compat=True
        unicode-letters|placement:Commerce/Orders/\u65e5\u672c|compat=True
        trailing-separator|DOTNETSP0015:Unsupported
        """);

    [Fact]
    void should_judge_every_hostile_source_owner_exactly() => string.Join('\n', _ownerResults).ShouldEqual(
        """
        plain|placement:Orders//Summary|compat=False
        leading-space|DOTNETSP0012:Conflict
        trailing-space|DOTNETSP0012:Conflict
        control-tab|DOTNETSP0012:Conflict
        empty|DOTNETSP0012:Conflict
        whitespace|DOTNETSP0012:Conflict
        unicode|placement:Orders//Summary|compat=False
        """);

    static ArtifactPlacement Placement() => new()
    {
        Module = "Commerce",
        Features = ["Orders"],
        Slice = "Place",
        SliceKind = GenerationSliceKind.StateChange
    };

    static DotNetSourcePlacementSnapshot Derive(ArtifactPlacement placement)
    {
        var subject = new SubjectId { Value = "dotnet://Ordering/Ordering/PlaceOrder" };
        return DotNetSourcePlacementDerivation.Derive(
        [
            new DotNetSourcePlacementRequest
            {
                Artifact = new ArtifactKey { Subject = subject, Kind = ArtifactKind.Command },
                Structure = new DotNetSourceStructure
                {
                    Subject = subject,
                    Project = "Ordering/Ordering",
                    ProjectRole = DotNetProjectRole.Application,
                    Namespace = "Ordering",
                    ProjectRelativePaths = ["PlaceOrder.cs"]
                },
                SliceKind = GenerationSliceKind.StateChange,
                Policy = new(),
                CompatibilityPolicy = new DotNetSourcePlacementCompatibilityPolicy { Placement = placement }
            }
        ]);
    }

    static DotNetSourcePlacementSnapshot DeriveWithOwner(string owner)
    {
        var ownerSubject = new SubjectId { Value = owner };
        return DotNetSourcePlacementDerivation.Derive(
        [
            new DotNetSourcePlacementRequest
            {
                Artifact = new ArtifactKey
                {
                    Subject = new SubjectId { Value = "dotnet://Ordering/Ordering/PlaceOrder#reducer" },
                    Kind = ArtifactKind.Reducer
                },
                Structure = new DotNetSourceStructure
                {
                    Subject = ownerSubject,
                    Project = "Ordering/Ordering",
                    ProjectRole = DotNetProjectRole.Application,
                    Namespace = "Application.Orders.Summary",
                    ProjectRelativePaths = ["Source/Orders/Summary/Summary.cs"]
                },
                SourceOwner = ownerSubject,
                SliceKind = GenerationSliceKind.StateView,
                Policy = new DotNetSourceStructurePolicy { FeatureRoot = "Source", NamespaceSegmentsToSkip = 1 }
            }
        ]);
    }

    static string Describe(DotNetSourcePlacementSnapshot snapshot) => snapshot.IsSuccess
        ? string.Join(
            ';',
            snapshot.Placements.Select(_ => Escape($"placement:{_.Placement.Module}/{string.Join('/', _.Placement.Features)}/{_.Placement.Slice}|compat={_.UsedCompatibilityPlacement}")))
        : string.Join(';', snapshot.Diagnostics.Select(_ => $"{_.Code}:{_.Outcome}"));

    static string Escape(string value) => string.Concat(value.Select(character =>
        char.IsControl(character) || char.IsSurrogate(character) || character > 0x7e
            ? $"\\u{(int)character:x4}"
            : character.ToString()));
}

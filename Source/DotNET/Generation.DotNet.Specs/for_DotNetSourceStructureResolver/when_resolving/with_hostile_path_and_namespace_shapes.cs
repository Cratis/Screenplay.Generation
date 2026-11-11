// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceStructureResolver.when_resolving;

public class with_hostile_path_and_namespace_shapes : Specification
{
    static readonly (string Label, string Value)[] _paths =
    [
        ("plain", "Accounts/Register/Register.cs"),
        ("backslash", "Accounts\\Register\\Register.cs"),
        ("mixed-separators", "Accounts\\Register/Register.cs"),
        ("leading-dot", "./Accounts/Register/Register.cs"),
        ("inner-dot", "Accounts/./Register/Register.cs"),
        ("double-separator", "Accounts//Register/Register.cs"),
        ("trailing-separator", "Accounts/Register/Register/"),
        ("only-file", "Register.cs"),
        ("only-dot", "."),
        ("only-separator", "/"),
        ("parent", "../Register.cs"),
        ("inner-parent", "Accounts/../Register/Register.cs"),
        ("backslash-parent", "Accounts\\..\\Register.cs"),
        ("encoded-dot-dot", "%2e%2e/Register/Register.cs"),
        ("recursively-encoded-dot-dot", "%252e%252e/Register/Register.cs"),
        ("encoded-separator", "Accounts%2FRegister/Register.cs"),
        ("rooted", "/Accounts/Register/Register.cs"),
        ("rooted-backslash", "\\Accounts\\Register\\Register.cs"),
        ("drive-slash", "C:/Accounts/Register/Register.cs"),
        ("drive-backslash", "C:\\Accounts\\Register\\Register.cs"),
        ("drive-relative", "c:Register.cs"),
        ("digit-drive", "1:/Accounts/Register.cs"),
        ("unicode-drive", "\u00e9:/Accounts/Register.cs"),
        ("empty", ""),
        ("whitespace", "   "),
        ("leading-space", " Accounts/Register/Register.cs"),
        ("trailing-space", "Accounts/Register/Register.cs "),
        ("inner-space-segment", "Accounts/ Register/Register.cs"),
        ("control-nul", "Accounts/Reg\u0000ister/Register.cs"),
        ("control-tab", "Accounts/Reg\tister/Register.cs"),
        ("unicode-precomposed", "Caf\u00e9/Register/Register.cs"),
        ("unicode-decomposed", "Cafe\u0301/Register/Register.cs"),
        ("unicode-letters", "\u65e5\u672c/Register/Register.cs"),
        ("lone-surrogate", "\ud800/Register/Register.cs")
    ];

    static readonly (string Label, string Value)[] _featureRoots =
    [
        ("plain", "Source"),
        ("backslash", "Source\\Accounts"),
        ("nested", "Source/Accounts"),
        ("leading-dot", "./Source"),
        ("double-separator", "Source//Accounts"),
        ("trailing-separator", "Source/"),
        ("parent", ".."),
        ("inner-parent", "Source/../Source"),
        ("encoded-dot-dot", "%2e%2e"),
        ("rooted", "/Source"),
        ("drive", "C:/Source"),
        ("digit-drive", "1:/Source"),
        ("empty", ""),
        ("whitespace", " "),
        ("padded-segment", "Source/ Accounts"),
        ("control", "Sour\u0001ce"),
        ("only-dot", "."),
        ("unicode-decomposed", "Cafe\u0301"),
        ("lone-surrogate", "\ud800")
    ];

    static readonly (string Label, string Value)[] _namespaces =
    [
        ("matching", "Banking.Accounts.Register"),
        ("empty", string.Empty),
        ("leading-dot", ".Banking.Accounts.Register"),
        ("trailing-dot", "Banking.Accounts.Register."),
        ("double-dot", "Banking..Accounts.Register"),
        ("only-dot", "."),
        ("dot-dot", ".."),
        ("padded-segment", "Banking. Accounts.Register"),
        ("trailing-space-segment", "Banking.Accounts .Register"),
        ("control", "Banking.Acc\u0001ounts.Register"),
        ("slash-in-segment", "Banking.Accounts/X.Register"),
        ("unicode-precomposed", "Banking.Caf\u00e9.Register"),
        ("unicode-decomposed", "Banking.Cafe\u0301.Register"),
        ("lone-surrogate", "Banking.\ud800.Register"),
        ("global-only-skipped", "Banking")
    ];

    string[] _pathResults = null!;
    string[] _featureRootResults = null!;
    string[] _namespaceResults = null!;

    void Because()
    {
        _pathResults = [.. _paths.Select(_ => $"{_.Label}|{Describe(Resolve("Banking.Accounts.Register", [_.Value], null))}")];
        _featureRootResults = [.. _featureRoots.Select(_ => $"{_.Label}|{Describe(Resolve("Banking.Accounts.Register", ["Source/Accounts/Register/Register.cs"], _.Value))}")];
        _namespaceResults = [.. _namespaces.Select(_ => $"{_.Label}|{Describe(Resolve(_.Value, ["Accounts/Register/Register.cs"], null))}")];
    }

    [Fact]
    void should_resolve_or_reject_every_hostile_source_path_exactly() => string.Join('\n', _pathResults).ShouldEqual(
        """
        plain|placement:Accounts//Register
        backslash|placement:Accounts//Register
        mixed-separators|placement:Accounts//Register
        leading-dot|placement:Accounts//Register
        inner-dot|placement:Accounts//Register
        double-separator|placement:Accounts//Register
        trailing-separator|placement:Accounts//Register
        only-file|placement:Accounts//Register
        only-dot|DOTNETSP0002:The project-relative source path '.' is invalid
        only-separator|DOTNETSP0002:The project-relative source path '/' is invalid
        parent|DOTNETSP0002:The project-relative source path '../Register.cs' is invalid
        inner-parent|DOTNETSP0002:The project-relative source path 'Accounts/../Register/Register.cs' is invalid
        backslash-parent|DOTNETSP0002:The project-relative source path 'Accounts\..\Register.cs' is invalid
        encoded-dot-dot|DOTNETSP0005:Folder placement '%2e%2e/Register' conflicts with namespace placement 'Accounts/Register'
        recursively-encoded-dot-dot|DOTNETSP0005:Folder placement '%252e%252e/Register' conflicts with namespace placement 'Accounts/Register'
        encoded-separator|placement:Accounts//Register
        rooted|DOTNETSP0002:The project-relative source path '/Accounts/Register/Register.cs' is invalid
        rooted-backslash|DOTNETSP0002:The project-relative source path '\Accounts\Register\Register.cs' is invalid
        drive-slash|DOTNETSP0002:The project-relative source path 'C:/Accounts/Register/Register.cs' is invalid
        drive-backslash|DOTNETSP0002:The project-relative source path 'C:\Accounts\Register\Register.cs' is invalid
        drive-relative|DOTNETSP0002:The project-relative source path 'c:Register.cs' is invalid
        digit-drive|DOTNETSP0005:Folder placement '1:/Accounts' conflicts with namespace placement 'Accounts/Register'
        unicode-drive|DOTNETSP0002:The project-relative source path '\u00e9:/Accounts/Register.cs' is invalid
        empty|DOTNETSP0002:The project-relative source path '' is invalid
        whitespace|DOTNETSP0002:The project-relative source path '   ' is invalid
        leading-space|DOTNETSP0002:The project-relative source path ' Accounts/Register/Register.cs' is invalid
        trailing-space|DOTNETSP0002:The project-relative source path 'Accounts/Register/Register.cs ' is invalid
        inner-space-segment|DOTNETSP0002:The project-relative source path 'Accounts/ Register/Register.cs' is invalid
        control-nul|DOTNETSP0002:The project-relative source path 'Accounts/Reg\u0000ister/Register.cs' is invalid
        control-tab|DOTNETSP0002:The project-relative source path 'Accounts/Reg\u0009ister/Register.cs' is invalid
        unicode-precomposed|DOTNETSP0005:Folder placement 'Caf\u00e9/Register' conflicts with namespace placement 'Accounts/Register'
        unicode-decomposed|DOTNETSP0005:Folder placement 'Caf\u00e9/Register' conflicts with namespace placement 'Accounts/Register'
        unicode-letters|DOTNETSP0005:Folder placement '\u65e5\u672c/Register' conflicts with namespace placement 'Accounts/Register'
        lone-surrogate|DOTNETSP0002:The project-relative source path '\ud800/Register/Register.cs' is invalid
        """);

    [Fact]
    void should_resolve_or_reject_every_hostile_feature_root_exactly() => string.Join('\n', _featureRootResults).ShouldEqual(
        """
        plain|placement:Accounts//Register
        backslash|placement:Accounts//Register
        nested|placement:Accounts//Register
        leading-dot|placement:Accounts//Register
        double-separator|placement:Accounts//Register
        trailing-separator|placement:Accounts//Register
        parent|DOTNETSP0002:The project-relative feature root '..' is invalid
        inner-parent|DOTNETSP0002:The project-relative feature root 'Source/../Source' is invalid
        encoded-dot-dot|DOTNETSP0003:The source path 'Source/Accounts/Register/Register.cs' is not beneath feature root '%2e%2e'
        rooted|DOTNETSP0002:The project-relative feature root '/Source' is invalid
        drive|DOTNETSP0002:The project-relative feature root 'C:/Source' is invalid
        digit-drive|DOTNETSP0003:The source path 'Source/Accounts/Register/Register.cs' is not beneath feature root '1:/Source'
        empty|DOTNETSP0002:The project-relative feature root '' is invalid
        whitespace|DOTNETSP0002:The project-relative feature root ' ' is invalid
        padded-segment|DOTNETSP0002:The project-relative feature root 'Source/ Accounts' is invalid
        control|DOTNETSP0002:The project-relative feature root 'Sour\u0001ce' is invalid
        only-dot|DOTNETSP0002:The project-relative feature root '.' is invalid
        unicode-decomposed|DOTNETSP0003:The source path 'Source/Accounts/Register/Register.cs' is not beneath feature root 'Cafe\u0301'
        lone-surrogate|DOTNETSP0002:The project-relative feature root '\ud800' is invalid
        """);

    [Fact]
    void should_resolve_or_reject_every_hostile_namespace_exactly() => string.Join('\n', _namespaceResults).ShouldEqual(
        """
        matching|placement:Accounts//Register
        empty|placement:Accounts//Register
        leading-dot|DOTNETSP0008:The declared namespace '.Banking.Accounts.Register' is invalid
        trailing-dot|DOTNETSP0008:The declared namespace 'Banking.Accounts.Register.' is invalid
        double-dot|DOTNETSP0008:The declared namespace 'Banking..Accounts.Register' is invalid
        only-dot|DOTNETSP0008:The declared namespace '.' is invalid
        dot-dot|DOTNETSP0008:The declared namespace '..' is invalid
        padded-segment|DOTNETSP0008:The declared namespace 'Banking. Accounts.Register' is invalid
        trailing-space-segment|DOTNETSP0008:The declared namespace 'Banking.Accounts .Register' is invalid
        control|DOTNETSP0008:The declared namespace 'Banking.Acc\u0001ounts.Register' is invalid
        slash-in-segment|DOTNETSP0005:Folder placement 'Accounts/Register' conflicts with namespace placement 'Accounts/X/Register'
        unicode-precomposed|DOTNETSP0005:Folder placement 'Accounts/Register' conflicts with namespace placement 'Caf\u00e9/Register'
        unicode-decomposed|DOTNETSP0005:Folder placement 'Accounts/Register' conflicts with namespace placement 'Caf\u00e9/Register'
        lone-surrogate|DOTNETSP0008:The declared namespace 'Banking.\ud800.Register' is invalid
        global-only-skipped|placement:Accounts//Register
        """);

    static DotNetSourceStructureResolution Resolve(string @namespace, string[] paths, string? featureRoot) =>
        DotNetSourceStructureResolver.Resolve(
            new DotNetSourceStructure
            {
                Subject = new() { Value = "dotnet://Banking/Banking/Accounts.Register.Register" },
                Project = "Banking",
                ProjectRole = DotNetProjectRole.Application,
                Namespace = @namespace,
                ProjectRelativePaths = paths
            },
            GenerationSliceKind.StateChange,
            new DotNetSourceStructurePolicy
            {
                FeatureRoot = featureRoot,
                NamespaceSegmentsToSkip = 1
            });

    static string Describe(DotNetSourceStructureResolution resolution) => resolution.IsSuccess
        ? Escape($"placement:{resolution.Placement.Module}/{string.Join('/', resolution.Placement.Features)}/{resolution.Placement.Slice}")
        : string.Join(';', resolution.Diagnostics.Select(_ => $"{_.Code}:{Escape(_.Message)}"));

    static string Escape(string value) => string.Concat(value.Select(character =>
        char.IsControl(character) || char.IsSurrogate(character) || character > 0x7e
            ? $"\\u{(int)character:x4}"
            : character.ToString()));
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetAdapterRunner;

public class when_legacy_project_order_depends_on_physical_or_ambiguous_paths : given.a_runner_context
{
    ModernAdapter[] _adapters = null!;
    AdapterRunSnapshot _forward = null!;
    AdapterRunSnapshot _reverse = null!;
    AdapterRunSnapshot _relocated = null!;
    AdapterRunSnapshot _ambiguous = null!;
    AdapterRunSnapshot _portable = null!;
    string _portableOrder = null!;

    void Because()
    {
        _adapters =
        [
            new ModernAdapter(Descriptor("forward", language: AdapterSourceLanguage.CSharp)),
            new ModernAdapter(Descriptor("reverse", language: AdapterSourceLanguage.CSharp)),
            new ModernAdapter(Descriptor("relocated", language: AdapterSourceLanguage.CSharp)),
            new ModernAdapter(Descriptor("ambiguous", language: AdapterSourceLanguage.CSharp)),
            new ModernAdapter(Descriptor("portable", language: AdapterSourceLanguage.CSharp))
        ];
        var first = Project("/checkout/one/Shared.csproj");
        var second = Project("/checkout/two/Shared.csproj");
        _forward = Run(_adapters[0], [first, second]);
        _reverse = Run(_adapters[1], [second, first]);
        _relocated = Run(
            _adapters[2],
            [Project("/private/relocated-a/Shared.csproj"), Project("/private/relocated-b/Shared.csproj")]);
        _ambiguous = Run(_adapters[3], [Project(null), Project(null)]);
        _adapters[4].OnProbe = context =>
        {
            _portableOrder = string.Join(',', context.Projects.Select(project => project.ProjectPath));
            return new AdapterProbeApplicable();
        };
        _portable = Run(_adapters[4], [Project("z/Shared.csproj"), Project("a/Shared.csproj")]);
    }

    [Fact] void should_reject_absolute_physical_project_path_ordering() => Code(_forward).ShouldEqual(DotNetAdapterGenerationDiagnosticCodes.InvalidProjectRoster);
    [Fact] void should_reject_ambiguous_legacy_project_ordering() => Code(_ambiguous).ShouldEqual(DotNetAdapterGenerationDiagnosticCodes.InvalidProjectRoster);
    [Fact] void should_keep_the_host_diagnostic_identical_under_reversed_input() => Projection(_reverse).ShouldEqual(Projection(_forward));
    [Fact] void should_keep_the_host_diagnostic_identical_after_checkout_relocation() => Projection(_relocated).ShouldEqual(Projection(_forward));
    [Fact] void should_not_leak_any_physical_project_path() => Projection(_forward).ShouldNotContain("checkout");
    [Fact] void should_validate_every_invalid_project_roster_before_probe_or_analysis() => _adapters.Take(4).All(adapter => adapter.DescriptorCount == 1 && adapter.ProbeCount == 0 && adapter.AnalyzeCount == 0).ShouldBeTrue();
    [Fact] void should_record_every_invalid_project_roster_adapter_as_blocked() => new[] { _forward, _reverse, _relocated, _ambiguous }.All(snapshot => snapshot.Adapters.Single().Disposition == AdapterRunDisposition.Blocked).ShouldBeTrue();
    [Fact] void should_continue_to_run_legacy_projects_disambiguated_by_portable_relative_paths() => _portable.Adapters.Single().Disposition.ShouldEqual(AdapterRunDisposition.Admitted);
    [Fact] void should_preserve_portable_relative_legacy_project_ordering() => _portableOrder.ShouldEqual("a/Shared.csproj,z/Shared.csproj");

    static AdapterRunSnapshot Run(ModernAdapter adapter, DotNetProjectCompilation[] projects) =>
        DotNetAdapterRunner.Run(
            [DotNetAdapterRegistration.For(adapter)],
            new DotNetAnalysisContext(projects),
            Options);

    static DotNetProjectCompilation Project(string? projectPath) => new()
    {
        Name = "Shared.Project",
        ProjectPath = projectPath,
        Compilation = CSharpCompilation.Create("Shared"),
        AuthoredSyntaxTrees = Enumerable.Empty<SyntaxTree>().ToHashSet()
    };

    static string Code(AdapterRunSnapshot snapshot) => snapshot.Diagnostics.Single().Code;

    static string Projection(AdapterRunSnapshot snapshot) => string.Join(
        '|',
        snapshot.Diagnostics.Select(diagnostic => $"{diagnostic.Code}:{diagnostic.Message}"));
}

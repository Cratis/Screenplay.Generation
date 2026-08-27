// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation.DotNet;

/// <summary>
/// Represents one opaque modern or legacy .NET adapter registration.
/// </summary>
public sealed class DotNetAdapterRegistration
{
    static readonly ImmutableArray<AdapterHostCapability> _legacyHostCapabilities =
    [
        AdapterHostCapability.AuthoredSource,
        AdapterHostCapability.SemanticAnalysis
    ];

    static readonly ImmutableArray<GenerationFactCapability> _legacyFactCapabilities =
    [
        .. Enum.GetValues<GenerationFactCapability>()
            .Where(capability => capability != GenerationFactCapability.Unknown)
            .OrderBy(capability => (int)capability)
    ];

    readonly IDescribedDotNetScreenplayAdapter? _modern;
    readonly IDotNetScreenplayAdapter? _legacy;

    DotNetAdapterRegistration(IDescribedDotNetScreenplayAdapter modern) => _modern = modern;

    DotNetAdapterRegistration(IDotNetScreenplayAdapter legacy) => _legacy = legacy;

    internal bool IsLegacy => _legacy is not null;

    /// <summary>
    /// Creates a registration for a described adapter.
    /// </summary>
    /// <param name="adapter">The described adapter.</param>
    /// <returns>An opaque adapter registration.</returns>
    public static DotNetAdapterRegistration For(IDescribedDotNetScreenplayAdapter adapter) => new(adapter);

    /// <summary>
    /// Creates a compatibility registration for an adapter implementing the original .NET adapter interface.
    /// </summary>
    /// <param name="adapter">The legacy adapter.</param>
    /// <returns>An opaque adapter registration.</returns>
    public static DotNetAdapterRegistration ForLegacy(IDotNetScreenplayAdapter adapter) => new(adapter);

    internal AdapterDescriptor Describe()
    {
        if (_modern is not null)
        {
            return _modern.Descriptor;
        }

        var identity = _legacy!.Identity;
        return new AdapterDescriptor
        {
            Identity = new AdapterIdentity { Id = identity.Id, Version = identity.Version },
            SourceLanguage = AdapterSourceLanguage.CSharp,
            Category = AdapterCategory.Legacy,
            RequiredHostCapabilities = _legacyHostCapabilities,
            EmittedFactCapabilities = _legacyFactCapabilities
        };
    }

    internal AdapterProbeResult Probe(DotNetAnalysisContext context)
    {
        if (_modern is not null)
        {
            return _modern.Probe(context);
        }

        return _legacy!.CanAnalyze(context)
            ? new AdapterProbeApplicable()
            : new AdapterProbeNotApplicable();
    }

    internal AdapterContribution Analyze(DotNetAnalysisContext context, DotNetAdapterOptions options) =>
        _modern is not null
            ? _modern.Analyze(context, options)
            : _legacy!.Analyze(context, options);
}

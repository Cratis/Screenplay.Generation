// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet;

/// <summary>
/// Defines stable diagnostics produced by the deterministic .NET adapter boundary.
/// </summary>
public static class DotNetAdapterGenerationDiagnosticCodes
{
    /// <summary>
    /// An adapter descriptor was rejected before probing.
    /// </summary>
    public const string DescriptorRejected = "DOTNETADAPTER001";

    /// <summary>
    /// An adapter identity was duplicated in the roster.
    /// </summary>
    public const string DuplicateAdapterId = "DOTNETADAPTER002";

    /// <summary>
    /// The .NET host cannot enforce a required adapter capability.
    /// </summary>
    public const string MissingHostCapability = "DOTNETADAPTER003";

    /// <summary>
    /// A modern adapter returned a malformed structured probe result.
    /// </summary>
    public const string ProbeRejected = "DOTNETADAPTER004";

    /// <summary>
    /// An applicable probe did not prove every required API capability.
    /// </summary>
    public const string MissingApiCapabilityEvidence = "DOTNETADAPTER005";

    /// <summary>
    /// An adapter callback threw an unexpected exception.
    /// </summary>
    public const string OperationFailed = "DOTNETADAPTER006";

    /// <summary>
    /// Atomic contribution admission rejected an adapter result.
    /// </summary>
    public const string ContributionRejected = "DOTNETADAPTER007";

    /// <summary>
    /// The registered source language cannot execute in the .NET runner.
    /// </summary>
    public const string UnsupportedSourceLanguage = "DOTNETADAPTER008";

    /// <summary>
    /// The adapter does not support the Generation.Contracts version loaded by the runner host.
    /// </summary>
    public const string IncompatibleGenerationVersion = "DOTNETADAPTER009";

    /// <summary>
    /// The project roster cannot be ordered without ambiguous or machine-specific identity.
    /// </summary>
    public const string InvalidProjectRoster = "DOTNETADAPTER010";
}

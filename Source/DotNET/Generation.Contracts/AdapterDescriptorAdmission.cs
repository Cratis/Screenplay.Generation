// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Represents the deeply frozen result of admitting an adapter descriptor.
/// </summary>
public sealed record AdapterDescriptorAdmissionResult
{
    /// <summary>
    /// Gets the deeply frozen descriptor, including when diagnostics reject it.
    /// </summary>
    public required AdapterDescriptor Descriptor { get; init; }

    /// <summary>
    /// Gets deterministic descriptor admission diagnostics.
    /// </summary>
    public ImmutableArray<AdapterContributionAdmissionDiagnostic> Diagnostics { get; init; } = [];

    /// <summary>
    /// Gets whether the descriptor was admitted.
    /// </summary>
    public bool IsAdmitted => Diagnostics.IsEmpty;
}

/// <summary>
/// Deeply freezes, canonicalizes, and validates source-neutral adapter descriptors.
/// </summary>
public static class AdapterDescriptorAdmission
{
    /// <summary>
    /// Deeply freezes, canonicalizes, and validates an adapter descriptor.
    /// </summary>
    /// <param name="descriptor">The descriptor to admit.</param>
    /// <returns>The frozen descriptor and deterministic admission diagnostics.</returns>
    public static AdapterDescriptorAdmissionResult Admit(AdapterDescriptor? descriptor)
    {
        var context = new AdapterContributionAdmissionContext();
        var frozen = AdapterContributionFreezer.FreezeDescriptor(descriptor, context);
        AdapterContributionAdmissionValidator.ValidateDescriptor(frozen, context);
        return new AdapterDescriptorAdmissionResult
        {
            Descriptor = frozen,
            Diagnostics = context.Diagnostics()
        };
    }
}

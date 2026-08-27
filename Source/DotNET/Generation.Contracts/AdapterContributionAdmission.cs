// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Deeply freezes and atomically admits source-neutral adapter contributions.
/// </summary>
public static class AdapterContributionAdmission
{
    /// <summary>
    /// Deeply freezes, canonically orders, and validates an adapter contribution without partially admitting facts.
    /// </summary>
    /// <param name="descriptor">The descriptor governing the contribution.</param>
    /// <param name="contribution">The untrusted contribution to admit.</param>
    /// <param name="sourceAuthorityValidator">The optional host source-authority validator.</param>
    /// <returns>An admitted immutable snapshot, or deterministic diagnostics with no admitted facts.</returns>
    public static AdapterContributionAdmissionResult Admit(
        AdapterDescriptor? descriptor,
        AdapterContribution? contribution,
        ISourceAuthorityValidator? sourceAuthorityValidator = null)
    {
        var context = new AdapterContributionAdmissionContext();
        var frozen = AdapterContributionFreezer.Freeze(descriptor, contribution, context);
        AdapterContributionAdmissionValidator.Validate(frozen, sourceAuthorityValidator, context);
        var diagnostics = context.Diagnostics();
        if (context.HasDiagnostics)
        {
            return new AdapterContributionAdmissionResult { Diagnostics = diagnostics };
        }

        return new AdapterContributionAdmissionResult
        {
            Snapshot = new AdapterContributionSnapshot
            {
                Descriptor = frozen.Descriptor,
                Facts = frozen.Facts,
                Diagnostics = frozen.Diagnostics
            }
        };
    }
}

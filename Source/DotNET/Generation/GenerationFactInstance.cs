// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Identifies one contributed fact instance: fact ids are unique only within one producer's contribution.
/// </summary>
/// <param name="Producer">The adapter that produced the fact.</param>
/// <param name="Id">The fact id within the producer's contribution.</param>
/// <param name="Subject">The subject the fact asserts about.</param>
sealed record GenerationFactInstance(AdapterIdentity? Producer, FactId? Id, SubjectId? Subject)
{
    /// <summary>
    /// Gets the instance key of a fact.
    /// </summary>
    /// <param name="fact">The fact.</param>
    /// <returns>The instance key.</returns>
    public static GenerationFactInstance Of(GenerationFact fact) => new(fact.Evidence?.Adapter, fact.Id, fact.Subject);

    /// <summary>
    /// Gets the instance key of the candidate a support fact names: the same producer and subject as the support.
    /// </summary>
    /// <param name="support">The support fact.</param>
    /// <returns>The candidate's instance key.</returns>
    public static GenerationFactInstance CandidateOf(MethodFactSupportFact support) => new(support.Evidence?.Adapter, support.Definition?.Candidate, support.Subject);
}

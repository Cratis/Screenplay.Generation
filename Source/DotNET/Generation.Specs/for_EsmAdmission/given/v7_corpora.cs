// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Generation.for_EsmAdmission.given;

public class v7_corpora : Specification
{
    protected CanonicalCorpusVector[] Corpora = [];
    protected byte[][] OriginalBytes = [];
    protected SemanticRevision[] OriginalRevisions = [];
    protected ExecutableSemanticModel[] ValidatedModels = [];

    void Establish()
    {
        Corpora = [RegisterProjectCorpus.V7, PolicyNegationCorpus.V7];
        OriginalBytes = [.. Corpora.Select(corpus => corpus.EsmBytes.ToArray())];
        OriginalRevisions = [.. Corpora.Select(corpus => corpus.SemanticRevision)];
        ValidatedModels = [.. Corpora.Select(corpus => SemanticModelSerializer.Deserialize(corpus.EsmBytes.AsSpan()))];
    }
}

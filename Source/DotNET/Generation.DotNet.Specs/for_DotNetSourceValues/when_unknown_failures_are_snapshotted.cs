// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceValues;

public class when_unknown_failures_are_snapshotted : Specification
{
    List<DotNetValueFailure> _source = null!;
    DotNetValueFailure _failure = null!;
    DotNetUnknown<int> _unknown = null!;

    void Establish()
    {
        _failure = new(DotNetValueFailureKind.Computed, Location.None, "Computed");
        _source = [_failure];
        _unknown = new(Failures: _source);
    }

    void Because() => _source.Clear();

    [Fact] void should_expose_an_immutable_failure_surface() => _unknown.Failures.GetType().ShouldEqual(typeof(ImmutableArray<DotNetValueFailure>));
    [Fact] void should_retain_the_original_failure_snapshot() => _unknown.Failures.SequenceEqual([_failure]).ShouldBeTrue();
}

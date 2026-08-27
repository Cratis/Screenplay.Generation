// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_admitting_reversed_inputs : given.a_contribution
{
    string _forward = null!;
    string _reverse = null!;
    AdapterContributionSnapshot _reversedSnapshot = null!;
    string _forwardRejected = null!;
    string _reverseRejected = null!;

    void Because()
    {
        var facts = EveryFact();
        var forward = Admit(Descriptor(), Contribution(facts)).Snapshot!;
        var reversedDescriptor = Descriptor() with
        {
            RequiredHostCapabilities = [.. Descriptor().RequiredHostCapabilities.Reverse()],
            EmittedFactCapabilities = [.. Descriptor().EmittedFactCapabilities.Reverse()]
        };
        _reversedSnapshot = Admit(reversedDescriptor, Contribution([.. facts.AsEnumerable().Reverse()])).Snapshot!;
        _forward = Project(forward);
        _reverse = Project(_reversedSnapshot);

        var malformed = facts.Select(fact => fact is ArtifactFact artifact
            ? artifact with { Definition = artifact.Definition with { Properties = null! } }
            : fact).ToArray();
        _forwardRejected = RejectedProjection(Admit(contribution: Contribution(malformed)));
        _reverseRejected = RejectedProjection(Admit(contribution: Contribution([.. malformed.AsEnumerable().Reverse()])));
    }

    [Fact] void should_produce_the_same_canonical_snapshot_projection() => _reverse.ShouldEqual(_forward);
    [Fact] void should_preserve_authored_property_order() => _reversedSnapshot.Facts.OfType<ArtifactFact>().Single().Definition.Properties.Select(property => property.Name).ShouldEqual(["second", "first"]);
    [Fact] void should_preserve_authored_value_path_order() => string.Join('|', _reversedSnapshot.Facts.OfType<SpecificationValueFact>().Single().Definition.Key.Path).ShouldEqual("arguments|name");
    [Fact] void should_order_rejected_freeze_diagnostics_independently_of_fact_order() => _reverseRejected.ShouldEqual(_forwardRejected);

    static string RejectedProjection(AdapterContributionAdmissionResult result) => string.Join(
        '|',
        result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}:{diagnostic.Path}:{diagnostic.Fact?.Value}:{diagnostic.Source?.Path}:{diagnostic.Source?.StartLine}:{diagnostic.Source?.StartColumn}"));

    static string Project(AdapterContributionSnapshot snapshot) => string.Join(
        '|',
        string.Join(',', snapshot.Descriptor.RequiredHostCapabilities.Select(value => ((int)value).ToString(CultureInfo.InvariantCulture))),
        string.Join(',', snapshot.Descriptor.EmittedFactCapabilities.Select(value => ((int)value).ToString(CultureInfo.InvariantCulture))),
        string.Join(',', snapshot.Facts.Select(fact => $"{fact.Id.Value}:{fact.Subject.Value}:{fact.GetType().Name}")),
        string.Join(',', snapshot.Facts.OfType<ArtifactFact>().Single().Definition.Properties.Select(property => property.Name)),
        string.Join(',', snapshot.Facts.OfType<SpecificationValueFact>().Single().Definition.Key.Path));
}

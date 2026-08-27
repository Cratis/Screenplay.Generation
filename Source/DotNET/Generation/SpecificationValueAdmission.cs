// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

internal sealed class SpecificationValueAdmission(IEnumerable<ResolvedSpecificationValue> values)
{
    readonly Dictionary<string, ResolvedSpecificationValue> _values = values.ToDictionary(
        value => Structural.SpecificationValueKey(value.Key),
        StringComparer.Ordinal);

    public bool TryAdmit(
        SpecificationValueKey key,
        SpecificationStepKey step,
        out AdmittedSpecificationValue? admitted) =>
        TryAdmit(key, step, new HashSet<string>(StringComparer.Ordinal), out admitted);

    static bool SameStep(SpecificationStepKey left, SpecificationStepKey right) =>
        Structural.SpecificationStepKey(left) == Structural.SpecificationStepKey(right);

    static bool IsValidShape(SpecificationValueDefinition definition)
    {
        var hasScalar = definition.Scalar is not null;
        var hasChildren = definition.Children.Count > 0;
        return definition.Kind switch
        {
            SpecificationValueKind.Null => !hasScalar && !hasChildren,
            SpecificationValueKind.Text or SpecificationValueKind.Number => hasScalar && !hasChildren,
            SpecificationValueKind.Boolean =>
                (string.Equals(definition.Scalar, "true", StringComparison.Ordinal) ||
                 string.Equals(definition.Scalar, "false", StringComparison.Ordinal)) && !hasChildren,
            SpecificationValueKind.Collection or SpecificationValueKind.Composite => !hasScalar,
            _ => false
        };
    }

    bool TryAdmit(
        SpecificationValueKey key,
        SpecificationStepKey step,
        HashSet<string> visiting,
        out AdmittedSpecificationValue? admitted)
    {
        admitted = null;
        var canonicalKey = Structural.SpecificationValueKey(key);
        if (!SameStep(key.Step, step) || !visiting.Add(canonicalKey) ||
            !_values.TryGetValue(canonicalKey, out var resolved) || resolved.IsConflicted)
        {
            return false;
        }

        var variant = resolved.Variants.Single();
        var definition = variant.Definition;
        if (!IsValidShape(definition) ||
            definition.Children.Select(Structural.SpecificationValueKey).Distinct(StringComparer.Ordinal).Count() != definition.Children.Count)
        {
            return false;
        }

        var children = new List<AdmittedSpecificationValue>();
        foreach (var child in definition.Children)
        {
            if (!TryAdmit(child, step, new HashSet<string>(visiting, StringComparer.Ordinal), out var admittedChild))
            {
                return false;
            }

            children.Add(admittedChild!);
        }

        admitted = new()
        {
            Definition = definition,
            Evidence = variant.Evidence,
            Children = children
        };
        return true;
    }
}

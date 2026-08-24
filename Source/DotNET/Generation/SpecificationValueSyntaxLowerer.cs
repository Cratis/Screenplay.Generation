// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Generation;

internal static class SpecificationValueSyntaxLowerer
{
    static readonly SourceLocation _generated = SourceLocation.Start;

    public static bool TryLowerQuery(
        string query,
        IReadOnlyList<AdmittedSpecificationValue> values,
        List<SpecificationQuerySyntax> queries)
    {
        var arguments = values
            .Where(value => value.Definition.Key.Path.Count == 2 && value.Definition.Key.Path[0] == "arguments")
            .Select(value => Mapping(value, 1))
            .ToArray();
        var resultValues = values
            .Where(value => value.Definition.Key.Path.Count == 3 && value.Definition.Key.Path[0] == "result")
            .Select(value => new
            {
                Index = ParseIndex(value.Definition.Key.Path[1]),
                Mapping = Mapping(value, 2)
            })
            .ToArray();
        if (values.Count != arguments.Length + resultValues.Length || resultValues.Any(value => value.Index is null))
        {
            return false;
        }

        var results = resultValues
            .GroupBy(value => value.Index!.Value)
            .OrderBy(group => group.Key)
            .Select(group => new SpecificationQueryResultSyntax(
                group.Select(value => value.Mapping),
                _generated))
            .ToArray();
        queries.Add(new(query, arguments, results, _generated));
        return true;
    }

    public static PropertyMappingSyntax Mapping(AdmittedSpecificationValue value) =>
        Mapping(value, value.Definition.Key.Path.Count - 1);

    public static bool IsScalar(AdmittedSpecificationValue value) => value.Definition.Kind is
        SpecificationValueKind.Null or SpecificationValueKind.Text or SpecificationValueKind.Number or SpecificationValueKind.Boolean;

    static PropertyMappingSyntax Mapping(AdmittedSpecificationValue value, int pathIndex) =>
        new(
            value.Definition.Key.Path[pathIndex],
            new LiteralExpressionSyntax(Scalar(value.Definition), _generated),
            _generated);

    static object? Scalar(SpecificationValueDefinition value) => value.Kind switch
    {
        SpecificationValueKind.Null => null,
        SpecificationValueKind.Text => value.Scalar,
        SpecificationValueKind.Number => decimal.Parse(value.Scalar!, NumberStyles.Number, CultureInfo.InvariantCulture),
        SpecificationValueKind.Boolean => bool.Parse(value.Scalar!),
        _ => null
    };

    static int? ParseIndex(string value) => int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var index)
        ? index
        : null;
}

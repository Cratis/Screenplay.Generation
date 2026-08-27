// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Screenplay.Generation;

static class AdapterContributionText
{
    public static bool IsRequired(string? value) => !string.IsNullOrWhiteSpace(value);

    public static bool IsNormalized(string? value, bool allowWhitespace)
    {
        if (string.IsNullOrEmpty(value) || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
        {
            return false;
        }

        if (value.Any(character => char.IsControl(character) || (!allowWhitespace && char.IsWhiteSpace(character))))
        {
            return false;
        }

        try
        {
            return string.Equals(value, value.Normalize(NormalizationForm.FormC), StringComparison.Ordinal);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}

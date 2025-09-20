using System.Globalization;
using System.Text.RegularExpressions;

namespace TrafficSimulation.Core.Extensions;

/// <summary>
/// Extension methods for string manipulation and formatting.
/// </summary>
public static class StringExtensions {
    private static readonly Regex s_SlugRegex = new(@"[^a-z0-9\-]", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
    private static readonly Regex s_MultipleHyphensRegex = new(@"-+", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    /// <summary>
    /// Converts a string to a URL-friendly slug.
    /// </summary>
    /// <param name="input">The input string to convert.</param>
    /// <returns>A URL-friendly slug.</returns>
    public static string ToSlug(this string input) {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Convert to lowercase and trim
        var slug = input.Trim().ToLowerInvariant();

        // Replace spaces and underscores with hyphens
        slug = slug.Replace(' ', '-').Replace('_', '-');

        // Remove accents and diacritics
        slug = RemoveAccents(slug);

        // Remove all non-alphanumeric characters except hyphens
        slug = s_SlugRegex.Replace(slug, string.Empty);

        // Replace multiple consecutive hyphens with a single hyphen
        slug = s_MultipleHyphensRegex.Replace(slug, "-");

        // Remove leading and trailing hyphens
        slug = slug.Trim('-');

        return slug;
    }

    public static string Indent(this string input, int spaces) {
        if (string.IsNullOrEmpty(input) || spaces <= 0)
            return input;

        var inputSpan = input.AsSpan().TrimStart();
        var stringBuilder = new StringBuilder();
        foreach (var range in inputSpan.Split('\n')) {
            var span = inputSpan[range].Trim("\r\n");
            if (span.IsEmpty) {
                stringBuilder.Append('\n');
                continue;
            }

            stringBuilder.Append(' ', spaces).Append(span).Append('\n');
        }

        return stringBuilder.ToString().TrimEnd('\n');
    }

    /// <summary>
    /// Removes accents and diacritics from a string.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <returns>A string with accents and diacritics removed.</returns>
    private static string RemoveAccents(string input) {
        var normalizedString = input.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString) {
            var unicodeCategory = char.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark) {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }
}

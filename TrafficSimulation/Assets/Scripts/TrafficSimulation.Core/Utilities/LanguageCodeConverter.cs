namespace TrafficSimulation.Core.Utilities;

/// <summary>
/// Utility class for converting between different language code formats.
/// Converts ISO 639-2/T (3-letter) and ISO 639-2/B codes to ISO 639-1 (2-letter) codes
/// for use with CultureInfo and other .NET internationalization features.
/// </summary>
public static class LanguageCodeConverter {
    /// <summary>
    /// Dictionary mapping 3-letter language codes to 2-letter ISO 639-1 codes.
    /// Includes the most common languages found in media files.
    /// </summary>
    private static readonly Dictionary<string, string> s_LanguageCodeMap = new(StringComparer.OrdinalIgnoreCase) {
        // Major European languages
        { "eng", "en" }, // English
        { "fre", "fr" }, { "fra", "fr" }, // French (both ISO 639-2/T and 639-2/B)
        { "ger", "de" }, { "deu", "de" }, // German (both ISO 639-2/T and 639-2/B)
        { "spa", "es" }, // Spanish
        { "ita", "it" }, // Italian
        { "por", "pt" }, // Portuguese
        { "dut", "nl" }, { "nld", "nl" }, // Dutch (both ISO 639-2/T and 639-2/B)
        { "rus", "ru" }, // Russian
        { "pol", "pl" }, // Polish
        { "swe", "sv" }, // Swedish
        { "nor", "no" }, // Norwegian
        { "dan", "da" }, // Danish
        { "fin", "fi" }, // Finnish
        { "ice", "is" }, { "isl", "is" }, // Icelandic (both ISO 639-2/T and 639-2/B)
        { "gre", "el" }, { "ell", "el" }, // Greek (both ISO 639-2/T and 639-2/B)
        { "hun", "hu" }, // Hungarian
        { "cze", "cs" }, { "ces", "cs" }, // Czech (both ISO 639-2/T and 639-2/B)
        { "slo", "sk" }, { "slk", "sk" }, // Slovak (both ISO 639-2/T and 639-2/B)
        { "slv", "sl" }, // Slovenian
        { "bul", "bg" }, // Bulgarian
        { "rom", "ro" }, { "ron", "ro" }, // Romanian (both ISO 639-2/T and 639-2/B)
        { "hrv", "hr" }, // Croatian
        { "srp", "sr" }, // Serbian
        { "bos", "bs" }, // Bosnian
        { "mkd", "mk" }, // Macedonian
        { "alb", "sq" }, { "sqi", "sq" }, // Albanian (both ISO 639-2/T and 639-2/B)
        { "lit", "lt" }, // Lithuanian
        { "lav", "lv" }, // Latvian
        { "est", "et" }, // Estonian
        { "ukr", "uk" }, // Ukrainian
        { "bel", "be" }, // Belarusian

        // Asian languages
        { "chi", "zh" }, { "zho", "zh" }, // Chinese (both ISO 639-2/T and 639-2/B)
        { "jpn", "ja" }, // Japanese
        { "kor", "ko" }, // Korean
        { "tha", "th" }, // Thai
        { "vie", "vi" }, // Vietnamese
        { "ind", "id" }, // Indonesian
        { "may", "ms" }, { "msa", "ms" }, // Malay (both ISO 639-2/T and 639-2/B)
        { "fil", "tl" }, // Filipino
        { "hin", "hi" }, // Hindi
        { "ben", "bn" }, // Bengali
        { "tam", "ta" }, // Tamil
        { "tel", "te" }, // Telugu
        { "guj", "gu" }, // Gujarati
        { "mar", "mr" }, // Marathi
        { "kan", "kn" }, // Kannada
        { "mal", "ml" }, // Malayalam
        { "pan", "pa" }, // Punjabi
        { "urd", "ur" }, // Urdu
        { "nep", "ne" }, // Nepali
        { "sin", "si" }, // Sinhala

        // Middle Eastern and African languages
        { "ara", "ar" }, // Arabic
        { "heb", "he" }, // Hebrew
        { "per", "fa" }, { "fas", "fa" }, // Persian (both ISO 639-2/T and 639-2/B)
        { "tur", "tr" }, // Turkish
        { "kur", "ku" }, // Kurdish
        { "arm", "hy" }, { "hye", "hy" }, // Armenian (both ISO 639-2/T and 639-2/B)
        { "geo", "ka" }, { "kat", "ka" }, // Georgian (both ISO 639-2/T and 639-2/B)
        { "aze", "az" }, // Azerbaijani
        { "amh", "am" }, // Amharic
        { "hau", "ha" }, // Hausa
        { "swa", "sw" }, // Swahili
        { "afr", "af" }, // Afrikaans

        // Other common languages
        { "cat", "ca" }, // Catalan
        { "baq", "eu" }, { "eus", "eu" }, // Basque (both ISO 639-2/T and 639-2/B)
        { "wel", "cy" }, { "cym", "cy" }, // Welsh (both ISO 639-2/T and 639-2/B)
        { "gle", "ga" }, // Irish
        { "gla", "gd" }, // Scottish Gaelic
        { "ltz", "lb" }, // Luxembourgish
    };

    /// <summary>
    /// Converts a language code to ISO 639-1 format (2-letter) if possible.
    /// </summary>
    /// <param name="languageCode">The language code to convert (can be 2 or 3 letters).</param>
    /// <returns>The ISO 639-1 (2-letter) language code, or the original code if no conversion is available.</returns>
    public static string? ToIso639_1(string? languageCode) {
        if (string.IsNullOrWhiteSpace(languageCode)) {
            return languageCode;
        }

        var code = languageCode.Trim();

        // If it's already a 2-letter code, return as-is
        if (code.Length == 2) {
            return code.ToLowerInvariant();
        }

        // If it's a 3-letter code, try to convert it
        if (code.Length == 3 && s_LanguageCodeMap.TryGetValue(code, out var converted)) {
            return converted;
        }

        // If we can't convert it, return the original code in lowercase
        return code.ToLowerInvariant();
    }

    /// <summary>
    /// Checks if the given language code is supported for conversion.
    /// </summary>
    /// <param name="languageCode">The language code to check.</param>
    /// <returns>True if the language code can be converted to ISO 639-1, false otherwise.</returns>
    public static bool IsSupported(string? languageCode) {
        if (string.IsNullOrWhiteSpace(languageCode)) {
            return false;
        }

        var code = languageCode.Trim();

        // 2-letter codes are always considered supported
        if (code.Length == 2) {
            return true;
        }

        // Check if 3-letter code is in our mapping
        return code.Length == 3 && s_LanguageCodeMap.ContainsKey(code);
    }

    /// <summary>
    /// Gets all supported 3-letter language codes.
    /// </summary>
    /// <returns>A collection of supported 3-letter language codes.</returns>
    public static IEnumerable<string> GetSupportedThreeLetterCodes() {
        return s_LanguageCodeMap.Keys.ToList();
    }

    /// <summary>
    /// Gets all supported 2-letter language codes.
    /// </summary>
    /// <returns>A collection of supported 2-letter language codes.</returns>
    public static IEnumerable<string> GetSupportedTwoLetterCodes() {
        return s_LanguageCodeMap.Values.Distinct().ToList();
    }
}

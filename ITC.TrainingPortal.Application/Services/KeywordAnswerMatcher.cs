using System.Text;
using System.Text.RegularExpressions;

namespace ITC.TrainingPortal.Application.Services;

public static class KeywordAnswerMatcher
{
    // Semicolons separate required words or phrases. Match complete tokens, never substrings.
    public static string[] Keywords(string? keywords) => (keywords ?? "").Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    public static string Normalize(string value) => string.Join(" ",
        Regex.Matches(value.Normalize(NormalizationForm.FormKC).ToUpperInvariant(), @"[\p{L}\p{M}\p{N}]+")
            .Select(x => x.Value));

    public static bool IsMatch(string? answer, string? keywords)
    {
        var required = Keywords(keywords).Select(Normalize).ToArray();
        var normalized = " " + Normalize(answer ?? "") + " ";
        return required.Length > 0 && required.All(x => x.Length > 0 && normalized.Contains(" " + x + " ", StringComparison.Ordinal));
    }
}

using System.Globalization;
using System.Text.Json;

namespace ClaimReader.Api.Services;

public static class ExtractionParser
{
    public static ExtractionResult Parse(string json)
    {
        var cleaned = Clean(json);
        using var doc = JsonDocument.Parse(cleaned);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
            throw new FormatException("Model output is not a JSON object.");

        return new ExtractionResult(
            GetString(root, "claimantName"),
            GetString(root, "policyNumber"),
            GetDecimal(root, "claimAmount"),
            GetDate(root, "incidentDate"),
            GetString(root, "description"),
            cleaned);
    }

    private static string Clean(string json)
    {
        var s = json.Trim();
        if (s.StartsWith("```"))
        {
            var firstNewLine = s.IndexOf('\n');
            if (firstNewLine >= 0) s = s[(firstNewLine + 1)..];
            s = s.TrimEnd('`', ' ', '\n', '\r');
        }
        return s;
    }

    private static string? GetString(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var p) || p.ValueKind != JsonValueKind.String) return null;
        var s = p.GetString()?.Trim();
        return string.IsNullOrEmpty(s) ? null : s;
    }

    private static decimal? GetDecimal(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var p)) return null;
        if (p.ValueKind == JsonValueKind.Number && p.TryGetDecimal(out var d)) return d;

        if (p.ValueKind == JsonValueKind.String)
        {
            // Keep digits, decimal point and minus sign; this drops "$", "CAD", spaces and thousands commas.
            var cleaned = new string(p.GetString()!
                .Where(c => char.IsDigit(c) || c == '.' || c == '-')
                .ToArray());

            if (decimal.TryParse(cleaned,
                    NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture, out var parsed))
                return parsed;
        }

        return null;
    }

    private static DateOnly? GetDate(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var p) || p.ValueKind != JsonValueKind.String) return null;
        return DateOnly.TryParseExact(p.GetString(), "yyyy-MM-dd",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;
    }
}
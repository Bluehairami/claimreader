using System.Text.RegularExpressions;
using ClaimReader.Api.Models;

namespace ClaimReader.Api.Services;

public static partial class ClaimValidator
{
    public const decimal MaxAmount = 1_000_000m;

    // Adjust to your own sample documents' policy format.
    [GeneratedRegex(@"^PM-\d{6}$", RegexOptions.IgnoreCase)]
    private static partial Regex PolicyPattern();

    public static List<string> Validate(Claim c, DateOnly? today = null) =>
        Validate(c.ClaimantName, c.PolicyNumber, c.ClaimAmount, c.IncidentDate, today);

    /// <summary>Returns the names of fields that are missing or invalid (camelCase, matching the JSON).</summary>
    public static List<string> Validate(string? name, string? policyNumber, decimal? amount,
        DateOnly? incidentDate, DateOnly? today = null)
    {
        var now = today ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var issues = new List<string>();

        if (string.IsNullOrWhiteSpace(name)) issues.Add("claimantName");

        if (string.IsNullOrWhiteSpace(policyNumber) || !PolicyPattern().IsMatch(policyNumber.Trim()))
            issues.Add("policyNumber");

        if (amount is null or <= 0 or > MaxAmount) issues.Add("claimAmount");

        if (incidentDate is null || incidentDate > now || incidentDate < now.AddYears(-10))
            issues.Add("incidentDate");

        return issues;
    }
}
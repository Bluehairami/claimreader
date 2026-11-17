using ClaimReader.Api.Services;

namespace ClaimReader.Tests;

public class ClaimValidatorTests
{
    private static readonly DateOnly Today = new(2026, 11, 4);
    private static readonly DateOnly GoodDate = new(2026, 9, 14);

    [Fact]
    public void ValidClaim_HasNoIssues()
    {
        var issues = ClaimValidator.Validate("Sarah Johnson", "PM-482913", 3450m, GoodDate, Today);
        Assert.Empty(issues);
    }

    [Fact]
    public void MissingEverything_FlagsAllFourFields()
    {
        var issues = ClaimValidator.Validate(null, null, null, null, Today);
        Assert.Equal(["claimantName", "policyNumber", "claimAmount", "incidentDate"], issues);
    }

    [Theory]
    [InlineData("")]
    [InlineData("PM-12345")]
    [InlineData("PM-4829133")]
    [InlineData("XX-482913")]
    public void BadPolicyNumber_IsFlagged(string policy)
    {
        var issues = ClaimValidator.Validate("Sarah Johnson", policy, 3450m, GoodDate, Today);
        Assert.Contains("policyNumber", issues);
    }

    [Fact]
    public void ZeroNegativeOrHugeAmount_IsFlagged()
    {
        Assert.Contains("claimAmount", ClaimValidator.Validate("A", "PM-482913", 0m, GoodDate, Today));
        Assert.Contains("claimAmount", ClaimValidator.Validate("A", "PM-482913", -5m, GoodDate, Today));
        Assert.Contains("claimAmount", ClaimValidator.Validate("A", "PM-482913", 5_000_000m, GoodDate, Today));
    }

    [Fact]
    public void FutureOrVeryOldDate_IsFlagged()
    {
        Assert.Contains("incidentDate", ClaimValidator.Validate("A", "PM-482913", 10m, Today.AddDays(1), Today));
        Assert.Contains("incidentDate", ClaimValidator.Validate("A", "PM-482913", 10m, Today.AddYears(-11), Today));
    }
}
using ClaimReader.Api.Services;
using Xunit;

namespace ClaimReader.Tests;

public class ExtractionParserTests
{
    [Fact]
    public void Parse_FullJson_ReadsAllFields()
    {
        var json = """
        {"claimantName":"Sarah Johnson","policyNumber":"PM-482913","claimAmount":3450.00,
         "incidentDate":"2026-09-14","description":"Rear-ended at a traffic light"}
        """;

        var r = ExtractionParser.Parse(json);

        Assert.Equal("Sarah Johnson", r.ClaimantName);
        Assert.Equal("PM-482913", r.PolicyNumber);
        Assert.Equal(3450m, r.ClaimAmount);
        Assert.Equal(new DateOnly(2026, 9, 14), r.IncidentDate);
        Assert.Equal("Rear-ended at a traffic light", r.Description);
    }

    [Fact]
    public void Parse_NullsAndMissingFields_BecomeNull()
    {
        var r = ExtractionParser.Parse("""{"claimantName":null,"policyNumber":"PM-482913"}""");

        Assert.Null(r.ClaimantName);
        Assert.Null(r.ClaimAmount);
        Assert.Null(r.IncidentDate);
        Assert.Equal("PM-482913", r.PolicyNumber);
    }

    [Fact]
    public void Parse_WrongDateFormat_BecomesNull()
    {
        var r = ExtractionParser.Parse("""{"incidentDate":"14/09/2026"}""");
        Assert.Null(r.IncidentDate);
    }

    [Fact]
    public void Parse_AmountAsString_IsParsed()
    {
        var r = ExtractionParser.Parse("""{"claimAmount":"$3,450.00"}""");
        Assert.Equal(3450m, r.ClaimAmount);
    }

    [Fact]
    public void Parse_MarkdownFencedJson_IsAccepted()
    {
        var r = ExtractionParser.Parse("```json\n{\"claimantName\":\"Sarah Johnson\"}\n```");
        Assert.Equal("Sarah Johnson", r.ClaimantName);
    }

    [Fact]
    public void Parse_InvalidJson_Throws()
    {
        Assert.ThrowsAny<Exception>(() => ExtractionParser.Parse("not json"));
    }
}
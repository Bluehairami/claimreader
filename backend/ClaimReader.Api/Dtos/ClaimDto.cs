using ClaimReader.Api.Models;
using ClaimReader.Api.Services;

namespace ClaimReader.Api.Dtos;

public record ClaimDto(
    int Id,
    string FileName,
    string Status,
    string? ClaimantName,
    string? PolicyNumber,
    decimal? ClaimAmount,
    DateOnly? IncidentDate,
    string? Description,
    List<string> Issues,
    DateTime CreatedAt)
{
    public static ClaimDto From(Claim c) => new(
        c.Id, c.FileName, c.Status.ToString(),
        c.ClaimantName, c.PolicyNumber, c.ClaimAmount, c.IncidentDate, c.Description,
        c.Status == ClaimStatus.Approved ? new List<string>() : ClaimValidator.Validate(c),
        c.CreatedAt);
}

public record ReviewRequest(
    string? ClaimantName,
    string? PolicyNumber,
    decimal? ClaimAmount,
    DateOnly? IncidentDate,
    string? Description);
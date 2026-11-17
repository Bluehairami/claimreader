namespace ClaimReader.Api.Services;

public record ExtractionResult(
    string? ClaimantName,
    string? PolicyNumber,
    decimal? ClaimAmount,
    DateOnly? IncidentDate,
    string? Description,
    string RawJson);

public interface IExtractionService
{
    Task<ExtractionResult> ExtractAsync(byte[] fileBytes, string contentType, CancellationToken ct = default);
}
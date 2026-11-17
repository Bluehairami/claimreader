using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ClaimReader.Api.Services;

public class GeminiOptions
{
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "gemini-2.5-flash";
}

public class GeminiExtractionService(
    HttpClient http,
    IOptions<GeminiOptions> options,
    ILogger<GeminiExtractionService> logger) : IExtractionService
{
    private const string Prompt = """
        You read insurance claim documents. Extract the fields below and return only JSON that matches the schema.
        Rules:
        - Use null for any field that is missing, unreadable or unclear. Never guess.
        - claimantName: the full name of the person making the claim.
        - policyNumber: exactly as printed on the document.
        - claimAmount: a number with no currency symbol and no thousands separators.
        - incidentDate: the date of the incident as YYYY-MM-DD. If the day/month order is ambiguous, use null.
        - description: one or two sentences describing what happened.
        """;

    private static readonly object ResponseSchema = new
    {
        type = "OBJECT",
        properties = new
        {
            claimantName = new { type = "STRING", nullable = true },
            policyNumber = new { type = "STRING", nullable = true },
            claimAmount = new { type = "NUMBER", nullable = true },
            incidentDate = new { type = "STRING", nullable = true },
            description = new { type = "STRING", nullable = true }
        }
    };

    public async Task<ExtractionResult> ExtractAsync(byte[] fileBytes, string contentType, CancellationToken ct = default)
    {
        var opts = options.Value;
        if (string.IsNullOrWhiteSpace(opts.ApiKey))
            throw new InvalidOperationException("Gemini API key is not configured.");

        var body = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = Prompt },
                        new { inline_data = new { mime_type = contentType, data = Convert.ToBase64String(fileBytes) } }
                    }
                }
            },
            generationConfig = new
            {
                responseMimeType = "application/json",
                responseSchema = ResponseSchema,
                temperature = 0
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"https://generativelanguage.googleapis.com/v1beta/models/{opts.Model}:generateContent");
        request.Headers.Add("x-goog-api-key", opts.ApiKey);
        request.Content = JsonContent.Create(body);

        using var response = await http.SendAsync(request, ct);
        var responseText = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            // Log only the status code: the response may echo document content.
            logger.LogError("Gemini request failed with status {Status}", (int)response.StatusCode);
            throw new HttpRequestException($"Gemini returned status {(int)response.StatusCode}.");
        }

        using var doc = JsonDocument.Parse(responseText);
        if (!doc.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            throw new InvalidOperationException("Gemini returned no candidates (the document may have been blocked).");

        var modelText = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
        if (string.IsNullOrWhiteSpace(modelText))
            throw new InvalidOperationException("Gemini returned an empty response.");

        return ExtractionParser.Parse(modelText);
    }
}
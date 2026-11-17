using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClaimReader.Api.Dtos;
using ClaimReader.Api.Services;

namespace ClaimReader.Tests;

public class ClaimsApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient client = factory.CreateClient();

    private static MultipartFormDataContent Upload(string fileName = "claim.pdf", string mime = "application/pdf")
    {
        var file = new ByteArrayContent([1, 2, 3, 4]);
        file.Headers.ContentType = new MediaTypeHeaderValue(mime);
        return new MultipartFormDataContent { { file, "file", fileName } };
    }

    [Fact]
    public async Task Upload_GoodExtraction_ReturnsExtractedClaim()
    {
        factory.Fake.Reset();

        var res = await client.PostAsync("/api/claims", Upload());

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var dto = await res.Content.ReadFromJsonAsync<ClaimDto>();
        Assert.Equal("Extracted", dto!.Status);
        Assert.Equal("PM-482913", dto.PolicyNumber);
        Assert.Empty(dto.Issues);
    }

    [Fact]
    public async Task Upload_MissingAmount_IsMarkedNeedsReview()
    {
        factory.Fake.Reset();
        factory.Fake.Result = FakeExtractionService.Good() with { ClaimAmount = null };

        var res = await client.PostAsync("/api/claims", Upload());

        var dto = await res.Content.ReadFromJsonAsync<ClaimDto>();
        Assert.Equal("NeedsReview", dto!.Status);
        Assert.Contains("claimAmount", dto.Issues);
    }

    [Fact]
    public async Task Upload_ExtractionThrows_IsMarkedFailed()
    {
        factory.Fake.Reset();
        factory.Fake.ToThrow = new HttpRequestException("boom");

        var res = await client.PostAsync("/api/claims", Upload());

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var dto = await res.Content.ReadFromJsonAsync<ClaimDto>();
        Assert.Equal("Failed", dto!.Status);
    }

    [Fact]
    public async Task Upload_WrongFileType_ReturnsBadRequest()
    {
        factory.Fake.Reset();

        var res = await client.PostAsync("/api/claims", Upload("notes.txt", "text/plain"));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Review_ValidCorrections_ApprovesClaim()
    {
        factory.Fake.Reset();
        factory.Fake.Result = FakeExtractionService.Good() with { ClaimAmount = 3460m };
        var created = await (await client.PostAsync("/api/claims", Upload())).Content.ReadFromJsonAsync<ClaimDto>();

        var review = new ReviewRequest("Sarah Johnson", "PM-482913", 3450m, new DateOnly(2026, 9, 14), "Rear-ended");
        var res = await client.PutAsJsonAsync($"/api/claims/{created!.Id}/review", review);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var dto = await res.Content.ReadFromJsonAsync<ClaimDto>();
        Assert.Equal("Approved", dto!.Status);
        Assert.Equal(3450m, dto.ClaimAmount);
    }

    [Fact]
    public async Task Review_InvalidValues_ReturnsBadRequest()
    {
        factory.Fake.Reset();
        var created = await (await client.PostAsync("/api/claims", Upload())).Content.ReadFromJsonAsync<ClaimDto>();

        var review = new ReviewRequest("Sarah Johnson", "BAD", -1m, new DateOnly(2026, 9, 14), null);
        var res = await client.PutAsJsonAsync($"/api/claims/{created!.Id}/review", review);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }
}
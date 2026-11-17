using ClaimReader.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ClaimReader.Tests;

public class FakeExtractionService : IExtractionService
{
    public ExtractionResult? Result { get; set; }
    public Exception? ToThrow { get; set; }

    public static ExtractionResult Good() => new(
        "Sarah Johnson", "PM-482913", 3450m, new DateOnly(2026, 9, 14),
        "Rear-ended at a traffic light", "{}");

    public void Reset() { Result = Good(); ToThrow = null; }

    public Task<ExtractionResult> ExtractAsync(byte[] fileBytes, string contentType, CancellationToken ct = default)
    {
        if (ToThrow is not null) throw ToThrow;
        return Task.FromResult(Result ?? Good());
    }
}

public class ApiFactory : WebApplicationFactory<ClaimReader.Api.ApiMarker> // Fully qualify 'Program' to resolve ambiguity
{
    public FakeExtractionService Fake { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("UseInMemoryDb", "true");
        builder.UseSetting("Storage:Path", Path.Combine(Path.GetTempPath(), "claimreader-tests"));

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IExtractionService>();
            services.AddSingleton<IExtractionService>(Fake);
        });
    }
}
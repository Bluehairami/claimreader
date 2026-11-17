using ClaimReader.Api.Data;
using ClaimReader.Api.Dtos;
using ClaimReader.Api.Models;
using ClaimReader.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClaimReader.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClaimsController(
    AppDbContext db,
    IExtractionService extractor,
    FileStorage storage,
    ILogger<ClaimsController> logger) : ControllerBase
{
    private const long MaxFileBytes = 10 * 1024 * 1024;

    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg"
    };

    private static string ContentTypeFor(string fileName) =>
        ContentTypes.GetValueOrDefault(Path.GetExtension(fileName), "application/octet-stream");

    [HttpGet]
    public async Task<ActionResult<List<ClaimDto>>> GetAll([FromQuery] ClaimStatus? status, CancellationToken ct)
    {
        var query = db.Claims.AsNoTracking();
        if (status is not null)
            query = query.Where(c => c.Status == status);

        var claims = await query.OrderByDescending(c => c.CreatedAt).ToListAsync(ct);
        return claims.Select(ClaimDto.From).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClaimDto>> GetById(int id, CancellationToken ct)
    {
        var claim = await db.Claims.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
        return claim is null ? NotFound() : ClaimDto.From(claim);
    }

    [HttpPost]
    [RequestSizeLimit(MaxFileBytes + 1024 * 1024)]
    public async Task<ActionResult<ClaimDto>> Upload(IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0)
            return BadRequest(new { error = "The uploaded file is empty." });
        if (file.Length > MaxFileBytes)
            return BadRequest(new { error = "The file is larger than 10 MB." });

        var extension = Path.GetExtension(file.FileName);
        if (!ContentTypes.TryGetValue(extension, out var contentType))
            return BadRequest(new { error = "Only PDF, PNG and JPG files are allowed." });

        byte[] bytes;
        using (var ms = new MemoryStream())
        {
            await file.CopyToAsync(ms, ct);
            bytes = ms.ToArray();
        }

        var claim = new Claim
        {
            FileName = Path.GetFileName(file.FileName),
            FilePath = await storage.SaveAsync(bytes, extension, ct)
        };
        db.Claims.Add(claim);
        await db.SaveChangesAsync(ct);

        await RunExtractionAsync(claim, bytes, contentType, ct);

        return CreatedAtAction(nameof(GetById), new { id = claim.Id }, ClaimDto.From(claim));
    }

    [HttpPost("{id:int}/extract")]
    public async Task<ActionResult<ClaimDto>> Reextract(int id, CancellationToken ct)
    {
        var claim = await db.Claims.FindAsync([id], ct);
        if (claim is null) return NotFound();
        if (claim.Status == ClaimStatus.Approved)
            return Conflict(new { error = "Approved claims cannot be re-extracted." });

        var path = storage.GetPath(claim.FilePath);
        if (!System.IO.File.Exists(path))
            return NotFound(new { error = "The stored file is missing." });

        var bytes = await System.IO.File.ReadAllBytesAsync(path, ct);
        await RunExtractionAsync(claim, bytes, ContentTypeFor(claim.FilePath), ct);
        return ClaimDto.From(claim);
    }

    [HttpPut("{id:int}/review")]
    public async Task<ActionResult<ClaimDto>> Review(int id, ReviewRequest request, CancellationToken ct)
    {
        var claim = await db.Claims.FindAsync([id], ct);
        if (claim is null) return NotFound();

        var issues = ClaimValidator.Validate(
            request.ClaimantName, request.PolicyNumber, request.ClaimAmount, request.IncidentDate);
        if (issues.Count > 0)
            return BadRequest(new { error = "Some fields are missing or invalid.", issues });

        claim.ClaimantName = request.ClaimantName!.Trim();
        claim.PolicyNumber = request.PolicyNumber!.Trim().ToUpperInvariant();
        claim.ClaimAmount = request.ClaimAmount;
        claim.IncidentDate = request.IncidentDate;
        claim.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        claim.Status = ClaimStatus.Approved;

        await db.SaveChangesAsync(ct);
        return ClaimDto.From(claim);
    }

    [HttpGet("{id:int}/file")]
    public async Task<IActionResult> GetFile(int id, CancellationToken ct)
    {
        var claim = await db.Claims.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
        if (claim is null) return NotFound();

        var path = storage.GetPath(claim.FilePath);
        if (!System.IO.File.Exists(path)) return NotFound();

        return PhysicalFile(path, ContentTypeFor(claim.FilePath));
    }

    private async Task RunExtractionAsync(Claim claim, byte[] bytes, string contentType, CancellationToken ct)
    {
        try
        {
            var result = await extractor.ExtractAsync(bytes, contentType, ct);

            claim.ClaimantName = result.ClaimantName;
            claim.PolicyNumber = result.PolicyNumber;
            claim.ClaimAmount = result.ClaimAmount;
            claim.IncidentDate = result.IncidentDate;
            claim.Description = result.Description;
            claim.RawModelJson = result.RawJson;

            claim.Status = ClaimValidator.Validate(claim).Count == 0
                ? ClaimStatus.Extracted
                : ClaimStatus.NeedsReview;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Extraction failed for claim {ClaimId}", claim.Id);
            claim.Status = ClaimStatus.Failed;
        }

        await db.SaveChangesAsync(ct);
    }
}
namespace ClaimReader.Api.Models
{
    public enum ClaimStatus { Uploaded, Extracted, NeedsReview, Approved, Failed }
    public class Claim
    {
        public int Id { get; set; }
        public string FileName { get; set; } = "";
        public string FilePath { get; set; } = "";
        public ClaimStatus Status { get; set; } = ClaimStatus.Uploaded;

        public string? ClaimantName { get; set; }
        public string? PolicyNumber { get; set; }
        public decimal? ClaimAmount { get; set; }
        public DateOnly? IncidentDate { get; set; }
        public string? Description { get; set; }

        public string? RawModelJson { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

using MediaService.Domain.Entities;

namespace MediaService.Application.DTOs
{
    public class MediaUploadDto
    {
        public required string ListingId { get; set; }
        public required IFormFile File { get; set; }
        public int Order { get; set; }
    }

    public class MediaResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string ListingId { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Order { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class BulkUploadResponseDto
    {
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<MediaResponseDto> UploadedFiles { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }
}


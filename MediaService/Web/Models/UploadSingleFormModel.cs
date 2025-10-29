using Microsoft.AspNetCore.Http;

namespace MediaService.Web.Models
{
    public class UploadSingleFormModel
    {
        public string ListingId { get; set; } = string.Empty;
        public IFormFile File { get; set; } = default!;
        public int Order { get; set; } = 0;
    }
}

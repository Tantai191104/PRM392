using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace MediaService.Web.Models
{
    public class UploadBulkFormModel
    {
        public string ListingId { get; set; } = string.Empty;
        public List<IFormFile> Files { get; set; } = new List<IFormFile>();
    }
}

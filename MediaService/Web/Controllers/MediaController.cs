using MediaService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediaService.Web.Controllers
{
    [ApiController]
    [Route("api/media")]
    public class MediaController : ControllerBase
    {
        private readonly IMediaAppService _mediaService;
        private readonly ILogger<MediaController> _logger;

        public MediaController(IMediaAppService mediaService, ILogger<MediaController> logger)
        {
            _mediaService = mediaService;
            _logger = logger;
        }

        /// <summary>
        /// Upload single file
        /// </summary>
        [HttpPost("upload")]
        [Authorize]
        public async Task<IActionResult> UploadFile([FromForm] string listingId, [FromForm] IFormFile file, [FromForm] int order = 0)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(listingId))
                    return BadRequest(new { success = false, message = "ListingId is required" });

                if (file == null)
                    return BadRequest(new { success = false, message = "File is required" });

                var result = await _mediaService.UploadFileAsync(listingId, file, order);
                return Ok(new { success = true, data = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                return StatusCode(500, new { success = false, message = "Error uploading file" });
            }
        }

        /// <summary>
        /// Upload multiple files for a listing
        /// </summary>
        [HttpPost("upload/bulk")]
        [Authorize]
        public async Task<IActionResult> BulkUpload([FromForm] string listingId, [FromForm] List<IFormFile> files)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(listingId))
                    return BadRequest(new { success = false, message = "ListingId is required" });

                if (files == null || files.Count == 0)
                    return BadRequest(new { success = false, message = "At least one file is required" });

                var result = await _mediaService.BulkUploadAsync(listingId, files);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk upload");
                return StatusCode(500, new { success = false, message = "Error uploading files" });
            }
        }

        /// <summary>
        /// Get media by ID
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var media = await _mediaService.GetMediaByIdAsync(id);
                if (media == null)
                    return NotFound(new { success = false, message = "Media not found" });

                return Ok(new { success = true, data = media });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting media {MediaId}", id);
                return StatusCode(500, new { success = false, message = "Error retrieving media" });
            }
        }

        /// <summary>
        /// Get all media for a listing
        /// </summary>
        [HttpGet("listing/{listingId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByListingId(string listingId)
        {
            try
            {
                var mediaList = await _mediaService.GetMediaByListingIdAsync(listingId);
                return Ok(new { success = true, data = mediaList, count = mediaList.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting media for listing {ListingId}", listingId);
                return StatusCode(500, new { success = false, message = "Error retrieving media" });
            }
        }

        /// <summary>
        /// Download file
        /// </summary>
        [HttpGet("{id}/download")]
        [AllowAnonymous]
        public async Task<IActionResult> DownloadFile(string id)
        {
            try
            {
                var media = await _mediaService.GetMediaByIdAsync(id);
                if (media == null)
                    return NotFound(new { success = false, message = "Media not found" });

                var fileBytes = await _mediaService.DownloadFileAsync(id);
                return File(fileBytes, media.ContentType, media.FileName);
            }
            catch (FileNotFoundException)
            {
                return NotFound(new { success = false, message = "File not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file {MediaId}", id);
                return StatusCode(500, new { success = false, message = "Error downloading file" });
            }
        }

        /// <summary>
        /// Serve file directly (for URL access)
        /// </summary>
        [HttpGet("files/{fileName}")]
        [AllowAnonymous]
        public async Task<IActionResult> ServeFile(string fileName)
        {
            try
            {
                var uploadPath = Path.Combine("/app/uploads", fileName);
                if (!System.IO.File.Exists(uploadPath))
                    return NotFound();

                var fileBytes = await System.IO.File.ReadAllBytesAsync(uploadPath);
                var contentType = GetContentType(fileName);
                return File(fileBytes, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error serving file {FileName}", fileName);
                return NotFound();
            }
        }

        /// <summary>
        /// Delete media
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var deleted = await _mediaService.DeleteMediaAsync(id);
                if (!deleted)
                    return NotFound(new { success = false, message = "Media not found" });

                return Ok(new { success = true, message = "Media deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting media {MediaId}", id);
                return StatusCode(500, new { success = false, message = "Error deleting media" });
            }
        }

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLower();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".mp4" => "video/mp4",
                ".mov" => "video/quicktime",
                _ => "application/octet-stream"
            };
        }
    }
}


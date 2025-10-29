using MediaService.Application.DTOs;
using MediaService.Domain.Entities;
using MediaService.Infrastructure.Repositories;
using MediaService.Infrastructure.Storage;

namespace MediaService.Application.Services
{
    public interface IMediaAppService
    {
        Task<MediaResponseDto> UploadFileAsync(string listingId, IFormFile file, int order = 0);
        Task<MediaResponseDto?> GetMediaByIdAsync(string id);
        Task<List<MediaResponseDto>> GetMediaByListingIdAsync(string listingId);
        Task<bool> DeleteMediaAsync(string id);
        Task<byte[]> DownloadFileAsync(string id);
        Task<BulkUploadResponseDto> BulkUploadAsync(string listingId, List<IFormFile> files);
    }

    public class MediaAppService : IMediaAppService
    {
        private readonly IMediaRepository _repository;
        private readonly IStorageService _storage;
        private readonly ILogger<MediaAppService> _logger;
        private readonly IConfiguration _configuration;

        // Allowed file types
        private readonly string[] _allowedImageTypes = { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
        private readonly string[] _allowedVideoTypes = { "video/mp4", "video/mpeg", "video/quicktime" };
        private readonly long _maxFileSize;

        public MediaAppService(
            IMediaRepository repository,
            IStorageService storage,
            ILogger<MediaAppService> logger,
            IConfiguration configuration)
        {
            _repository = repository;
            _storage = storage;
            _logger = logger;
            _configuration = configuration;
            _maxFileSize = _configuration.GetValue<long>("Storage:MaxFileSizeMB", 10) * 1024 * 1024; // Default 10MB
        }

        public async Task<MediaResponseDto> UploadFileAsync(string listingId, IFormFile file, int order = 0)
        {
            // Validate file
            ValidateFile(file);

            // Determine media type
            var mediaType = DetermineMediaType(file.ContentType);

            // Generate unique filename
            var extension = Path.GetExtension(file.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";

            // Save file to storage
            var storagePath = await _storage.SaveFileAsync(file, uniqueFileName);

            // Get URL
            var url = _storage.GetFileUrl(storagePath);

            // Create media entity
            var media = new Media
            {
                ListingId = listingId,
                Url = url,
                Type = mediaType,
                Order = order,
                FileName = file.FileName,
                FileSize = file.Length,
                ContentType = file.ContentType,
                StoragePath = storagePath
            };

            // Save to database
            await _repository.CreateAsync(media);

            _logger.LogInformation("Uploaded media {MediaId} for listing {ListingId}", media.Id, listingId);

            return MapToDto(media);
        }

        public async Task<MediaResponseDto?> GetMediaByIdAsync(string id)
        {
            var media = await _repository.GetByIdAsync(id);
            return media != null ? MapToDto(media) : null;
        }

        public async Task<List<MediaResponseDto>> GetMediaByListingIdAsync(string listingId)
        {
            var mediaList = await _repository.GetByListingIdAsync(listingId);
            return mediaList.Select(MapToDto).ToList();
        }

        public async Task<bool> DeleteMediaAsync(string id)
        {
            var media = await _repository.GetByIdAsync(id);
            if (media == null)
                return false;

            // Delete from storage
            await _storage.DeleteFileAsync(media.StoragePath);

            // Delete from database
            var deleted = await _repository.DeleteAsync(id);

            if (deleted)
                _logger.LogInformation("Deleted media {MediaId}", id);

            return deleted;
        }

        public async Task<byte[]> DownloadFileAsync(string id)
        {
            var media = await _repository.GetByIdAsync(id);
            if (media == null)
                throw new FileNotFoundException($"Media with ID {id} not found");

            return await _storage.GetFileAsync(media.StoragePath);
        }

        public async Task<BulkUploadResponseDto> BulkUploadAsync(string listingId, List<IFormFile> files)
        {
            var response = new BulkUploadResponseDto();

            for (int i = 0; i < files.Count; i++)
            {
                try
                {
                    var mediaDto = await UploadFileAsync(listingId, files[i], i);
                    response.UploadedFiles.Add(mediaDto);
                    response.SuccessCount++;
                }
                catch (Exception ex)
                {
                    response.FailureCount++;
                    response.Errors.Add($"File {files[i].FileName}: {ex.Message}");
                    _logger.LogError(ex, "Error uploading file {FileName}", files[i].FileName);
                }
            }

            return response;
        }

        private void ValidateFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty");

            if (file.Length > _maxFileSize)
                throw new ArgumentException($"File size exceeds maximum allowed size of {_maxFileSize / 1024 / 1024}MB");

            var contentType = file.ContentType.ToLower();
            if (!_allowedImageTypes.Contains(contentType) && !_allowedVideoTypes.Contains(contentType))
                throw new ArgumentException($"File type {contentType} is not allowed");
        }

        private MediaType DetermineMediaType(string contentType)
        {
            contentType = contentType.ToLower();
            if (_allowedImageTypes.Contains(contentType))
                return MediaType.IMAGE;
            if (_allowedVideoTypes.Contains(contentType))
                return MediaType.VIDEO;

            throw new ArgumentException($"Unsupported content type: {contentType}");
        }

        private MediaResponseDto MapToDto(Media media)
        {
            return new MediaResponseDto
            {
                Id = media.Id,
                ListingId = media.ListingId,
                Url = media.Url,
                Type = media.Type.ToString(),
                Order = media.Order,
                FileName = media.FileName,
                FileSize = media.FileSize,
                ContentType = media.ContentType,
                CreatedAt = media.CreatedAt
            };
        }
    }
}


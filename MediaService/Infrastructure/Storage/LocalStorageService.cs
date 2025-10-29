namespace MediaService.Infrastructure.Storage
{
    public class LocalStorageService : IStorageService
    {
        private readonly string _uploadPath;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LocalStorageService> _logger;

        public LocalStorageService(IConfiguration configuration, ILogger<LocalStorageService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _uploadPath = _configuration.GetValue<string>("Storage:UploadPath") ?? "/app/uploads";

            // Create upload directory if not exists
            if (!Directory.Exists(_uploadPath))
            {
                Directory.CreateDirectory(_uploadPath);
                _logger.LogInformation("Created upload directory: {Path}", _uploadPath);
            }
        }

        public async Task<string> SaveFileAsync(IFormFile file, string fileName)
        {
            var filePath = Path.Combine(_uploadPath, fileName);

            try
            {
                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);
                _logger.LogInformation("Saved file: {FileName} to {Path}", fileName, filePath);
                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving file: {FileName}", fileName);
                throw;
            }
        }

        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    await Task.Run(() => File.Delete(filePath));
                    _logger.LogInformation("Deleted file: {Path}", filePath);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {Path}", filePath);
                return false;
            }
        }

        public async Task<byte[]> GetFileAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException($"File not found: {filePath}");

                return await File.ReadAllBytesAsync(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading file: {Path}", filePath);
                throw;
            }
        }

        public string GetFileUrl(string filePath)
        {
            var baseUrl = _configuration.GetValue<string>("Storage:BaseUrl") ?? "http://localhost:5140";
            var fileName = Path.GetFileName(filePath);
            return $"{baseUrl}/api/media/files/{fileName}";
        }
    }
}


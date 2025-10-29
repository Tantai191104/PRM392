namespace MediaService.Infrastructure.Storage
{
    public interface IStorageService
    {
        Task<string> SaveFileAsync(IFormFile file, string fileName);
        Task<bool> DeleteFileAsync(string filePath);
        Task<byte[]> GetFileAsync(string filePath);
        string GetFileUrl(string filePath);
    }
}


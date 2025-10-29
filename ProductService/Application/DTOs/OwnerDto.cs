namespace ProductService.Application.DTOs
{
    public class OwnerDto
    {
        // Id intentionally not used in API responses
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
    }
}
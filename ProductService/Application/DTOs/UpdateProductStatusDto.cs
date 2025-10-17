namespace ProductService.Application.DTOs
{
    public class UpdateProductStatusDto
    {
        public string Status { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }
}
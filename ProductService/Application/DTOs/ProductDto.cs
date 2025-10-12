namespace ProductService.Application.DTOs
{
    public class ProductDto
    {
        public string Name { get; set; } = null!;
        public decimal Price { get; set; }
        public Dictionary<string, object> Attributes { get; set; } = new();
    }
}

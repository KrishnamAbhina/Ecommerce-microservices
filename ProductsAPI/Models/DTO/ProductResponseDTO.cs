namespace ProductsAPI.Models.DTO
{
    public class ProductResponseDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? ImagePath { get; set; }
        public int Quantity { get; set; }
        public bool IsActive { get; set; }
        
    }
}

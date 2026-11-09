using System.ComponentModel.DataAnnotations;

namespace CartAPI.MODELS.DTO
{
    public class CartDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "Product is required.")]
        public int ProductId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; }
    }

    public class CartResponseDTO
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
    }
}

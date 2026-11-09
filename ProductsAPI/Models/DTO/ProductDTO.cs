using System.ComponentModel.DataAnnotations;

namespace ProductsAPI.Models.DTO
{
    public class ProductDTO
    {
        [Required(ErrorMessage = "Product name is required.")]
        [StringLength(150, MinimumLength = 3,
            ErrorMessage = "Product name must be between 3 and 150 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string? Description { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Price cannot be negative.")]
        public decimal Price { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Category is required.")]
        public int CategoryId { get; set; }

        [StringLength(500, ErrorMessage = "Image path cannot exceed 500 characters.")]
        public string? ImagePath { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Quantity cannot be negative.")]
        public int Quantity { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using OrdersAPI.Models;

namespace OrdersAPI.Models.DTO
{
    public class OrderRequestDTO
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Address { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        [EnumDataType(typeof(PaymentMode))]
        public PaymentMode? PaymentMode { get; set; }
    }
}

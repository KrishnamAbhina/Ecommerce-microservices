using System.Text.Json.Serialization;
using OrdersAPI.Models;

namespace OrdersAPI.Models.DTO
{
    public class OrderResponseDTO
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? Description { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PaymentMode PaymentMode { get; set; }
        public int NumberOfItems { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<OrderItemResponseDTO> Items { get; set; } = new();
    }

    public class OrderItemResponseDTO
    {
        public int Id { get; set; }
        public int CartId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
    }
}

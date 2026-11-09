using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrdersAPI.Models
{
    [Table("OrderItems")]
    public class OrderItemModel
    {
        [Key]
        public int Id { get; set; }

        public int OrderId { get; set; }

        public OrderModel Order { get; set; } = null!;

        public int CartId { get; set; }

        public int ProductId { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }
    }
}

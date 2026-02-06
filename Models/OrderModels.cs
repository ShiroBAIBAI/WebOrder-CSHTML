using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Demo.Models
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        [Precision(18, 2)]
        public decimal Total { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [MaxLength(100)]
        public string? MemberEmail { get; set; }   // nullable

        public List<OrderLine> Lines { get; set; } = new();

        //  Extra fields
        [MaxLength(20)]
        public string? TableNo { get; set; }

        [MaxLength(20)]
        public string? Status { get; set; }   // Pending | In Progress | Completed

        [MaxLength(50)]
        public string? PaymentMethod { get; set; }

        [MaxLength(50)]
        public string? PaymentChannel { get; set; }

        [Precision(18, 2)]
        public decimal Subtotal { get; set; }

        [Precision(18, 2)]
        public decimal Discount { get; set; }

        [Precision(18, 2)]
        public decimal Tax { get; set; }

        [MaxLength(50)]
        public string? VoucherCode { get; set; }

        [MaxLength(64)]
        public string? IdempotencyKey { get; set; }
    }

    public class OrderLine
    {
        [Key]
        public int OrderLineId { get; set; }

        public int OrderId { get; set; }

        [MaxLength(6)]
        public string MenuItemId { get; set; } = default!;
        [MaxLength(20)]
        public string LineStatus { get; set; } = "Placed";

        public int Quantity { get; set; }   //  instead of Qty

        [Precision(18, 2)]
        public decimal Price { get; set; }  //  with precision

        [MaxLength(200)]
        public string? Options { get; set; }

        public Order Order { get; set; } = default!;

        public MenuItem MenuItem { get; set; } = default!;
    }

    public class Cart
    {
        [Key]
        public int CartId { get; set; }

        [MaxLength(100)]
        public string MemberEmail { get; set; } = default!;

        public List<CartLine> Lines { get; set; } = new();
    }

    public class CartLine
    {
        [Key]
        public int CartLineId { get; set; }

        public int CartId { get; set; }

        [MaxLength(6)]
        public string MenuItemId { get; set; } = default!;

        public int Quantity { get; set; }

        [MaxLength(200)]
        public string? Options { get; set; }

        // Navigation
        public Cart Cart { get; set; } = default!;
        public MenuItem MenuItem { get; set; } = default!;
    }
}


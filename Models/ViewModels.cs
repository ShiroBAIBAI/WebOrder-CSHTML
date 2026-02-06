using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Demo.Models;

#nullable disable warnings

public class LoginVM
{
    [Required, StringLength(100)]
    [EmailAddress]
    public string Email { get; set; }

    [Required, StringLength(100, MinimumLength = 5)]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    public bool RememberMe { get; set; }

    [FromForm(Name = "g-recaptcha-response")]
    public string CaptchaToken { get; set; }
}

public class RegisterVM
{
    [StringLength(100)]
    [EmailAddress]
    [Remote("CheckEmail", "Account", ErrorMessage = "Duplicated {0}.")]
    public string Email { get; set; }

    [StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    [StringLength(100, MinimumLength = 8)]
    [Compare("Password")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    public string Confirm { get; set; }

    [StringLength(100)]
    public string Name { get; set; }

    public IFormFile Photo { get; set; }

    [StringLength(6, MinimumLength = 4)]
    [Display(Name = "Verification Code")]
    public string? Code { get; set; }
}

public class UpdatePasswordVM
{
    [Required]
    [StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password)]
    [Display(Name = "Current Password")]
    public string Current { get; set; } = "";

    [Required]
    [StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string New { get; set; } = "";

    [Required]
    [StringLength(100, MinimumLength = 8)]
    [Compare("New")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    public string Confirm { get; set; } = "";
}

public class UpdateProfileVM
{
    public string? Email { get; set; }

    [StringLength(100)]
    public string Name { get; set; }

    public string? PhotoURL { get; set; }

    public IFormFile? Photo { get; set; }
}

public class ResetPasswordVM
{
    [StringLength(100)]
    [EmailAddress]
    public string Email { get; set; }
}

// Reservation

public class ReserveVM
{
    [Display(Name = "Room Type")]
    public string TypeId { get; set; }

    [Display(Name = "Check In Date")]
    [DataType(DataType.Date)]
    public DateOnly CheckIn { get; set; }

    [Display(Name = "Check Out Date")]
    [DataType(DataType.Date)]
    public DateOnly CheckOut { get; set; }
}

//waikin
public class CartItem
{
    public MenuItem Item { get; set; }
    public int Quantity { get; set; }
    public decimal Subtotal => Item.Price * Quantity;
    public string? Options { get; set; }
}
//jiali
public class PriceBreakdown
{
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal TaxRate { get; set; }  
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public string? VoucherCode { get; set; }
}

public class OrderSummaryVM
{
    public Dictionary<string, CartItem> Cart { get; set; } = new();
    public PriceBreakdown Price { get; set; } = new();
    public string? VoucherCode { get; set; }
    public PaymentMethod Payment { get; set; } = PaymentMethod.Cashier;
}

public enum PaymentMethod { Cashier, Online }

public class VoucherDef
{
    public string Code { get; set; } = "";
    public string Type { get; set; } = "percent"; 
    public decimal Amount { get; set; }
    public decimal? MinSpend { get; set; }
    public DateTime? ExpireAt { get; set; }
    public bool Active { get; set; } = true;
}
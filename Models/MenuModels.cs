using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.EntityFrameworkCore;

namespace Demo.Models
{
    public class MenuCategory
    {
        [Key]
        public int MenuCategoryId { get; set; } 

        [Required, MaxLength(100)]
        public string Name { get; set; } = default!;

        public int SortOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        // Navigation property (one-to-many)
        [ValidateNever]
        public List<MenuItem> MenuItems { get; set; } = new();
    }

    public class MenuItem
    {
        [Key, MaxLength(6)]
        [BindNever]
        public string MenuItemId { get; set; } = default!; 

        [Required]
        public int MenuCategoryId { get; set; }  // Foreign Key -> MenuCategory

        [Required, MaxLength(120)]
        public string Name { get; set; } = default!;

        [MaxLength(400)]
        public string? Description { get; set; }

        [Precision(10, 2)]
        [Range(0, 9999)]
        public decimal Price { get; set; }

        public bool IsAvailable { get; set; } = true;

        [MaxLength(40)]
        public string? Tag { get; set; }

        public string? Recipe { get; set; }

        [Display(Name = "Display Order")]
        [Range(1, 9999, ErrorMessage = "Please enter a number ≥ 1.")]
        public int SortOrder { get; set; } = 1;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ValidateNever]
        public MenuCategory Category { get; set; } = default!;

        [ValidateNever]
        public List<MenuItemImage> Images { get; set; } = new();
    }

    public class MenuItemImage
    {
        [Key, MaxLength(6)]
        public string ImageId { get; set; } = default!; // Example: IM0001

        [Required, MaxLength(6)]
        public string MenuItemId { get; set; } = default!; // FK -> MenuItem

        [Required, MaxLength(200)]
        public string Url { get; set; } = default!; // /uploads/xxx.jpg

        public int SortOrder { get; set; } = 0;

        // Navigation
        [ValidateNever]
        public MenuItem MenuItem { get; set; } = default!;
    }
}

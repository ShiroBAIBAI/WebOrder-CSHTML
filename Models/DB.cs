using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Demo.Models;

#nullable disable warnings

public class DB : DbContext
{
    public DB(DbContextOptions options) : base(options) { }

    // DB Sets
    public DbSet<User> Users { get; set; }
    public DbSet<Admin> Admins { get; set; }
    public DbSet<Member> Members { get; set; }
    public DbSet<Kitchen> Kitchens { get; set; }
    public DbSet<Bar> Bars { get; set; }
    public DbSet<Type> Types { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<Reservation> Reservations { get; set; }

    public DbSet<MenuCategory> MenuCategories { get; set; }
    public DbSet<MenuItem> MenuItems { get; set; }
    public DbSet<MenuItemImage> MenuItemImages { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderLine> OrderLines { get; set; }
    public DbSet<StockItem> StockItems { get; set; }
    public DbSet<InventoryTxn> InventoryTxns { get; set; }
    public DbSet<SystemSetting> SystemSettings { get; set; }
    public DbSet<Voucher> Vouchers { get; set; }




    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        //mb.Entity<MenuCategory>()
        //  .HasIndex(x => x.SortOrder)
        //  .IsUnique();

        //mb.Entity<Kitchen>();
        //mb.Entity<Bar>();

        //mb.Entity<MenuItem>()
        //  .Property(x => x.Price)
        //  .HasPrecision(10, 2);

        //mb.Entity<MenuItem>()
        //  .HasIndex(x => new { x.MenuCategoryId, x.SortOrder })
        //  .IsUnique();

        //mb.Entity<MenuItem>()
        //  .HasIndex(x => new { x.MenuCategoryId, x.IsAvailable });

        //mb.Entity<MenuItem>()
        //  .HasOne(i => i.Category)
        //  .WithMany(c => c.MenuItems)
        //  .HasForeignKey(i => i.MenuCategoryId)
        //  .OnDelete(DeleteBehavior.Cascade);

        //mb.Entity<MenuItemImage>()
        //  .HasIndex(x => new { x.MenuItemId, x.SortOrder });

        //mb.Entity<MenuItemImage>()
        //  .HasOne(img => img.MenuItem)
        //  .WithMany(i => i.Images)
        //  .HasForeignKey(img => img.MenuItemId)
        //  .OnDelete(DeleteBehavior.Cascade);

        //mb.Entity<OrderLine>()
        //  .HasOne(l => l.MenuItem)
        //  .WithMany()
        //  .HasForeignKey(l => l.MenuItemId);

        mb.Entity<Order>()
          .HasIndex(o => o.IdempotencyKey)
          .IsUnique()
          .HasFilter("[IdempotencyKey] IS NOT NULL");
    }
}

// ====================== Entity Classes ======================
public class User
{
    [Key, MaxLength(100)]
    public string Email { get; set; }
    [MaxLength(100)]
    public string Hash { get; set; }
    [MaxLength(100)]
    public string Name { get; set; }
    public int AccessFailedCount { get; set; } = 0;
    public DateTime? LockoutEnd { get; set; } = null;

    public string Role => GetType().Name;
}

public class Admin : User { }

public class Member : User
{
    [MaxLength(100)]
    public string PhotoURL { get; set; }

    public List<Reservation> Reservations { get; set; } = new();
}

public class Kitchen : User { }
public class Bar : User { }

public class StockItem
{
    [Key, MaxLength(6)]
    public string StockItemId { get; set; } = default!; 

    [Required, MaxLength(120)]
    public string Name { get; set; } = default!;

    [MaxLength(20)]
    public string? Unit { get; set; }

    [Range(0, 999999)]
    public int Quantity { get; set; }

    [Display(Name = "Reorder Level")]
    [Range(0, 999999)]
    public int ReorderLevel { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    [MaxLength(80)]
    public string? Category { get; set; }

    // Nav
    public List<InventoryTxn> Txns { get; set; } = new();
}

public class InventoryTxn
{
    [Key]
    public int InventoryTxnId { get; set; }

    [Required, MaxLength(6)]
    public string StockItemId { get; set; } = default!;

    public int QtyChange { get; set; }

    [MaxLength(20)]
    public string TxnType { get; set; } = "ADJUST"; // PURCHASE / CONSUME / ADJUST

    [Precision(12, 2)]
    public decimal? UnitCost { get; set; } 

    [MaxLength(200)]
    public string? Remark { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    public StockItem StockItem { get; set; } = default!;
}

public class SystemSetting
{
    [Key, MaxLength(100)]
    public string Key { get; set; } = default!;

    [MaxLength(400)]
    public string? Value { get; set; }
}

public class Voucher
{
    [Key, MaxLength(30)]
    public string Code { get; set; } = default!; 

    [MaxLength(10)]
    public string Type { get; set; } = "percent"; // "percent" | "fixed"

    [Precision(10, 2)]
    public decimal Amount { get; set; }

    [Precision(10, 2)]
    public decimal? MinSpend { get; set; }

    public DateTime? ExpireAt { get; set; }

    public bool Active { get; set; } = true;
}

// Dont delete, may casue many many problems
public class Type
{
    [Key, MaxLength(1)]
    public string Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; }

    [Precision(6, 2)]
    public decimal Price { get; set; }

    // Navigation Properties
    public List<Room> Rooms { get; set; } = new();
}

public class Room
{
    [Key, MaxLength(4)]
    public string Id { get; set; }

    // Foreign Keys
    public string TypeId { get; set; }

    // Navigation Properties
    public Type Type { get; set; }
    public List<Reservation> Reservations { get; set; } = new();
}

public class Reservation
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }

    [Precision(6, 2)]
    public decimal Price { get; set; }

    public bool Paid { get; set; }

    // Foreign Keys (MemberEmail, RoomId)
    public string MemberEmail { get; set; }
    public string RoomId { get; set; }

    // Navigation Properties
    public Member Member { get; set; }
    public Room Room { get; set; }
}
//until here
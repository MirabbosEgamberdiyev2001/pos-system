using Microsoft.EntityFrameworkCore;
using POS.Domain.Entities.Selling;
using POS.Domain.Entities;
using POS.Domain.Entities.Auth;

namespace POS.Domain.DataContext;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductItem> ProductItems { get; set; }
    public DbSet<Receipt> Receipts { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // --- Relationships ---
        builder.Entity<Category>()
               .HasMany(c => c.Products)
               .WithOne(p => p.Category)
               .HasForeignKey(p => p.CategoryId)
               .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Product>()
               .HasMany(p => p.ProductItems)
               .WithOne(p => p.Product)
               .HasForeignKey(p => p.ProductId)
               .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Receipt>()
               .HasOne(r => r.Seller)
               .WithMany()
               .HasForeignKey(r => r.SellerId)
               .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Receipt>()
               .HasMany(r => r.Transactions)
               .WithOne()
               .HasForeignKey(t => t.ReceiptId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProductItem>()
               .HasOne(p => p.Admin)
               .WithMany()
               .HasForeignKey(p => p.AdminId)
               .OnDelete(DeleteBehavior.NoAction);

        // --- Decimal precision ---
        builder.Entity<Product>().Property(p => p.Amount).HasPrecision(18, 4);
        builder.Entity<Product>().Property(p => p.WarningAmount).HasPrecision(18, 4);

        builder.Entity<ProductItem>().Property(p => p.Amount).HasPrecision(18, 4);
        builder.Entity<ProductItem>().Property(p => p.BuyingPrice).HasPrecision(18, 2);
        builder.Entity<ProductItem>().Property(p => p.SellingPrice).HasPrecision(18, 2);

        builder.Entity<Receipt>().Property(r => r.TotalPrice).HasPrecision(18, 2);
        builder.Entity<Receipt>().Property(r => r.PaidCash).HasPrecision(18, 2);
        builder.Entity<Receipt>().Property(r => r.PaidCard).HasPrecision(18, 2);

        builder.Entity<Transaction>().Property(t => t.ProductPrice).HasPrecision(18, 2);
        builder.Entity<Transaction>().Property(t => t.TotalPrice).HasPrecision(18, 2);

        // --- Phase 5A: Category.Name — DB constraint nvarchar(50) + index ---
        builder.Entity<Category>()
               .Property(c => c.Name)
               .HasMaxLength(50)
               .IsRequired();
        builder.Entity<Category>()
               .HasIndex(c => c.Name)
               .HasDatabaseName("IX_Categories_Name");

        // --- Phase 5C: Product.Barcode — unique index ---
        builder.Entity<Product>()
               .Property(p => p.Barcode)
               .HasMaxLength(13);
        builder.Entity<Product>()
               .HasIndex(p => p.Barcode)
               .IsUnique()
               .HasFilter("[Barcode] IS NOT NULL")
               .HasDatabaseName("IX_Products_Barcode_Unique");

        // --- Seed data ---
        builder.Entity<Category>()
            .HasData(new Category
            {
                Id               = 1,
                Name             = "Default Category",
                IsDeleted        = false,
                LastModifiedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });

        base.OnModelCreating(builder);
    }
}

using AkilliDepo.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace AkilliDepo.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
    public DbSet<GoodsReceiptItem> GoodsReceiptItems => Set<GoodsReceiptItem>();
    public DbSet<Box> Boxes => Set<Box>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<StoreOrder> StoreOrders => Set<StoreOrder>();
    public DbSet<StoreOrderItem> StoreOrderItems => Set<StoreOrderItem>();
    public DbSet<DispatchOrder> DispatchOrders => Set<DispatchOrder>();
    public DbSet<DispatchOrderItem> DispatchOrderItems => Set<DispatchOrderItem>();
    public DbSet<DispatchBox> DispatchBoxes => Set<DispatchBox>();
    public DbSet<DispatchBoxItem> DispatchBoxItems => Set<DispatchBoxItem>();
    public DbSet<DispatchPallet> DispatchPallets => Set<DispatchPallet>();
    public DbSet<DispatchPalletBox> DispatchPalletBoxes => Set<DispatchPalletBox>();
    public DbSet<User> Users => Set<User>();
    public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();
    public DbSet<WeeklyReport> WeeklyReports => Set<WeeklyReport>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<CompanySettings> CompanySettings => Set<CompanySettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Soft delete: tüm sorgulara IsDeleted == false filtresi (Global Query Filter)
        modelBuilder.Entity<Brand>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Product>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<GoodsReceipt>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<GoodsReceiptItem>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Box>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Location>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<StoreOrder>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<StoreOrderItem>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<DispatchOrder>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<DispatchOrderItem>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<DispatchBox>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<DispatchBoxItem>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<DispatchPallet>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<DispatchPalletBox>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<StockAdjustment>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<WeeklyReport>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Store>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<CompanySettings>().HasQueryFilter(e => !e.IsDeleted);

        // Marka adı şirket bazında benzersiz
        modelBuilder.Entity<Brand>()
            .HasIndex(b => new { b.CompanyId, b.Name })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Marka kısaltması (ShortCode) şirket bazında benzersiz
        modelBuilder.Entity<Brand>()
            .HasIndex(b => new { b.CompanyId, b.ShortCode })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Barkodlar şirket bazında benzersiz; soft-delete edilmiş kayıtlar indeksi bloklamasın
        modelBuilder.Entity<Product>()
            .HasIndex(p => new { p.CompanyId, p.Barcode })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Konum koordinatı (koridor/bölge/raf) şirket bazında benzersiz
        modelBuilder.Entity<Location>()
            .HasIndex(l => new { l.CompanyId, l.CorridorNo, l.ZoneNo, l.ShelfNo })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        modelBuilder.Entity<Box>()
            .HasIndex(b => new { b.CompanyId, b.Barcode })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        modelBuilder.Entity<Box>()
            .Property(b => b.Desi)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Location>()
            .HasOne(l => l.CurrentBox)
            .WithMany()
            .HasForeignKey(l => l.CurrentBoxId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DispatchBox>()
            .HasIndex(d => new { d.CompanyId, d.Barcode })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        modelBuilder.Entity<StoreOrder>()
            .HasIndex(o => new { o.CompanyId, o.OrderCode })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        modelBuilder.Entity<DispatchPallet>()
            .HasIndex(p => new { p.CompanyId, p.Barcode })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        modelBuilder.Entity<User>()
            .HasIndex(u => new { u.CompanyId, u.Username })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        modelBuilder.Entity<StoreOrderItem>()
            .HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DispatchOrder>()
            .HasOne(o => o.StoreOrder)
            .WithMany()
            .HasForeignKey(o => o.StoreOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DispatchOrderItem>()
            .HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DispatchPalletBox>()
            .HasOne(pb => pb.DispatchBox)
            .WithMany()
            .HasForeignKey(pb => pb.DispatchBoxId)
            .OnDelete(DeleteBehavior.Restrict);

        // Dağıtımda stok düşen kaynak koli silinmesin diye cascade kapalı
        modelBuilder.Entity<DispatchBoxItem>()
            .HasOne(i => i.SourceBox)
            .WithMany()
            .HasForeignKey(i => i.SourceBoxId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DispatchBoxItem>()
            .HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Product>()
            .HasOne(p => p.Brand)
            .WithMany(b => b.Products)
            .HasForeignKey(p => p.BrandId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<GoodsReceiptItem>()
            .HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<GoodsReceiptItem>()
            .HasOne(i => i.Brand)
            .WithMany()
            .HasForeignKey(i => i.BrandId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<GoodsReceiptItem>()
            .HasOne(i => i.Box)
            .WithMany()
            .HasForeignKey(i => i.BoxId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockAdjustment>()
            .HasOne(a => a.Box)
            .WithMany()
            .HasForeignKey(a => a.BoxId)
            .OnDelete(DeleteBehavior.Restrict);

        // Mağaza adı şirket bazında benzersiz (aynı mağazanın farklı ID'lerle tekrar oluşmasını önler).
        modelBuilder.Entity<Store>()
            .HasIndex(s => new { s.CompanyId, s.Name })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        modelBuilder.Entity<Store>()
            .HasIndex(s => new { s.CompanyId, s.StoreCode })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        modelBuilder.Entity<CompanySettings>()
            .HasIndex(s => s.CompanyId)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
    }
}

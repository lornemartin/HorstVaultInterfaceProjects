using HorstMFG.Core.Entities;
using HorstMFG.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace HorstMFG.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Plant> Plants => Set<Plant>();
    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Part> Parts => Set<Part>();
    public DbSet<BomImportBatch> BomImportBatches => Set<BomImportBatch>();
    public DbSet<BomLineItem> BomLineItems => Set<BomLineItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Nest> Nests => Set<Nest>();
    public DbSet<NestedPart> NestedParts => Set<NestedPart>();
    public DbSet<RadanIdAssignment> RadanIdAssignments => Set<RadanIdAssignment>();
    public DbSet<PdfDocument> PdfDocuments => Set<PdfDocument>();
    public DbSet<SystemConfiguration> SystemConfigurations => Set<SystemConfiguration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Plant
        modelBuilder.Entity<Plant>(e =>
        {
            e.ToTable("plants");
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).HasMaxLength(100).IsRequired();
            e.Property(p => p.Code).HasMaxLength(10).IsRequired();
            e.HasIndex(p => p.Code).IsUnique();
        });

        // ApplicationUser
        modelBuilder.Entity<ApplicationUser>(e =>
        {
            e.ToTable("users");
            e.HasKey(u => u.Id);
            e.Property(u => u.UserName).HasMaxLength(100).IsRequired();
            e.Property(u => u.PasswordHash).HasMaxLength(200).IsRequired();
            e.Property(u => u.FullName).HasMaxLength(200).IsRequired();
            e.HasIndex(u => u.UserName).IsUnique();
            e.HasOne(u => u.Plant).WithMany(p => p.Users).HasForeignKey(u => u.PlantId);
        });

        // UserRole
        modelBuilder.Entity<UserRole>(e =>
        {
            e.ToTable("user_roles");
            e.HasKey(r => r.Id);
            e.Property(r => r.RoleName).HasConversion<string>().HasMaxLength(50).IsRequired();
            e.HasOne(r => r.User).WithMany(u => u.Roles).HasForeignKey(r => r.UserId);
            e.HasIndex(r => new { r.UserId, r.RoleName }).IsUnique();
        });

        // Part
        modelBuilder.Entity<Part>(e =>
        {
            e.ToTable("parts");
            e.HasKey(p => p.Id);
            e.Property(p => p.Number).HasMaxLength(100).IsRequired();
            e.Property(p => p.FileName).HasMaxLength(255);
            e.Property(p => p.Title).HasMaxLength(255);
            e.Property(p => p.Description).HasMaxLength(1000);
            e.Property(p => p.Category).HasConversion<string>().HasMaxLength(50);
            e.Property(p => p.Material).HasMaxLength(100);
            e.Property(p => p.Thickness).HasPrecision(10, 4);
            e.Property(p => p.StructCode).HasMaxLength(50);
            e.Property(p => p.Operations).HasMaxLength(500);
            e.Property(p => p.Keywords).HasMaxLength(500);
            e.Property(p => p.LifecycleState).HasMaxLength(50);
            e.Property(p => p.Notes).HasMaxLength(2000);
            e.HasIndex(p => p.Number).IsUnique();
        });

        // BomImportBatch
        modelBuilder.Entity<BomImportBatch>(e =>
        {
            e.ToTable("bom_import_batches");
            e.HasKey(b => b.Id);
            e.Property(b => b.Name).HasMaxLength(200).IsRequired();
            e.Property(b => b.BomType).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.HasOne(b => b.Plant).WithMany(p => p.BomImportBatches).HasForeignKey(b => b.PlantId);
            e.HasOne(b => b.ImportedByUser).WithMany(u => u.ImportedBatches).HasForeignKey(b => b.ImportedByUserId);
        });

        // BomLineItem
        modelBuilder.Entity<BomLineItem>(e =>
        {
            e.ToTable("bom_line_items");
            e.HasKey(l => l.Id);
            e.Property(l => l.Number).HasMaxLength(100).IsRequired();
            e.Property(l => l.ParentNumber).HasMaxLength(100);
            e.HasOne(l => l.Batch).WithMany(b => b.LineItems).HasForeignKey(l => l.BatchId);
            e.HasOne(l => l.Parent).WithMany(l => l.Children).HasForeignKey(l => l.ParentId);
            e.HasOne(l => l.Part).WithMany(p => p.BomLineItems).HasForeignKey(l => l.PartId);
        });

        // Order
        modelBuilder.Entity<Order>(e =>
        {
            e.ToTable("orders");
            e.HasKey(o => o.Id);
            e.Property(o => o.OrderNumber).HasMaxLength(50).IsRequired();
            e.Property(o => o.ProductNumber).HasMaxLength(100);
            e.HasIndex(o => o.OrderNumber).IsUnique();
            e.HasOne(o => o.Batch).WithMany(b => b.Orders).HasForeignKey(o => o.BatchId);
            e.HasOne(o => o.Plant).WithMany(p => p.Orders).HasForeignKey(o => o.PlantId);
        });

        // OrderItem
        modelBuilder.Entity<OrderItem>(e =>
        {
            e.ToTable("order_items");
            e.HasKey(i => i.Id);
            e.Property(i => i.Notes).HasMaxLength(1000);
            e.HasOne(i => i.Order).WithMany(o => o.Items).HasForeignKey(i => i.OrderId);
            e.HasOne(i => i.Part).WithMany(p => p.OrderItems).HasForeignKey(i => i.PartId);
        });

        // Nest
        modelBuilder.Entity<Nest>(e =>
        {
            e.ToTable("nests");
            e.HasKey(n => n.Id);
            e.Property(n => n.NestName).HasMaxLength(200).IsRequired();
            e.Property(n => n.NestPath).HasMaxLength(500);
            e.HasOne(n => n.Plant).WithMany(p => p.Nests).HasForeignKey(n => n.PlantId);
        });

        // NestedPart
        modelBuilder.Entity<NestedPart>(e =>
        {
            e.ToTable("nested_parts");
            e.HasKey(np => np.Id);
            e.HasOne(np => np.Nest).WithMany(n => n.NestedParts).HasForeignKey(np => np.NestId);
            e.HasOne(np => np.OrderItem).WithMany(oi => oi.NestedParts).HasForeignKey(np => np.OrderItemId);
        });

        // RadanIdAssignment
        modelBuilder.Entity<RadanIdAssignment>(e =>
        {
            e.ToTable("radan_id_assignments");
            e.HasKey(r => r.Id);
            e.HasOne(r => r.OrderItem).WithOne(oi => oi.RadanIdAssignment).HasForeignKey<RadanIdAssignment>(r => r.OrderItemId);
            e.HasOne(r => r.Plant).WithMany(p => p.RadanIdAssignments).HasForeignKey(r => r.PlantId);
            e.HasIndex(r => new { r.RadanIdNumber, r.PlantId }).IsUnique();
        });

        // PdfDocument
        modelBuilder.Entity<PdfDocument>(e =>
        {
            e.ToTable("pdf_documents");
            e.HasKey(d => d.Id);
            e.Property(d => d.FileName).HasMaxLength(255).IsRequired();
            e.Property(d => d.FilePath).HasMaxLength(500).IsRequired();
            e.Property(d => d.Department).HasMaxLength(100);
            e.HasOne(d => d.Part).WithMany(p => p.PdfDocuments).HasForeignKey(d => d.PartId);
            e.HasOne(d => d.Batch).WithMany(b => b.PdfDocuments).HasForeignKey(d => d.BatchId);
        });

        // SystemConfiguration
        modelBuilder.Entity<SystemConfiguration>(e =>
        {
            e.ToTable("system_configuration");
            e.HasKey(c => c.Id);
            e.Property(c => c.Key).HasMaxLength(100).IsRequired();
            e.Property(c => c.Value).HasMaxLength(2000).IsRequired();
            e.Property(c => c.Description).HasMaxLength(500);
            e.HasIndex(c => c.Key).IsUnique();
        });
    }
}

using HorstMFG.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace HorstMFG.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Plant> Plants => Set<Plant>();
    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Part> Parts => Set<Part>();
    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<BatchProduct> BatchProducts => Set<BatchProduct>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<ScheduleOrder> ScheduleOrders => Set<ScheduleOrder>();
    public DbSet<PartLineItem> PartLineItems => Set<PartLineItem>();
    public DbSet<NestOrder> NestOrders => Set<NestOrder>();
    public DbSet<NestBatch> NestBatches => Set<NestBatch>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<BatchItem> BatchItems => Set<BatchItem>();
    public DbSet<Nest> Nests => Set<Nest>();
    public DbSet<NestedPart> NestedParts => Set<NestedPart>();
    public DbSet<RadanIdAssignment> RadanIdAssignments => Set<RadanIdAssignment>();
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
            e.Property(p => p.FileName).HasMaxLength(255);
            e.Property(p => p.Description).HasMaxLength(1000);
            e.Property(p => p.Material).HasMaxLength(100);
            e.Property(p => p.Thickness).HasPrecision(10, 4);
        });

        // Batch
        modelBuilder.Entity<Batch>(e =>
        {
            e.ToTable("batches");
            e.HasKey(b => b.Id);
            e.Property(b => b.Name).HasMaxLength(200).IsRequired();
            e.Property(b => b.LocalPdfFolder).HasMaxLength(500);
            e.HasOne(b => b.Plant).WithMany(p => p.Batches).HasForeignKey(b => b.PlantId);
            e.HasOne(b => b.ImportedByUser).WithMany().HasForeignKey(b => b.ImportedByUserId);
        });

        // BatchProduct
        modelBuilder.Entity<BatchProduct>(e =>
        {
            e.ToTable("batch_products");
            e.HasKey(bp => bp.Id);
            e.Property(bp => bp.ProductName).HasMaxLength(200).IsRequired();
            e.Property(bp => bp.Qty).HasColumnName("qty").HasDefaultValue(1);
            e.HasOne(bp => bp.Batch).WithMany(b => b.BatchProducts).HasForeignKey(bp => bp.BatchId);
        });

        // Schedule
        modelBuilder.Entity<Schedule>(e =>
        {
            e.ToTable("schedules");
            e.HasKey(s => s.Id);
            e.Property(s => s.Name).HasMaxLength(200).IsRequired();
            e.Property(s => s.LocalPdfFolder).HasMaxLength(500);
            e.HasOne(s => s.Plant).WithMany(p => p.Schedules).HasForeignKey(s => s.PlantId);
            e.HasOne(s => s.ImportedByUser).WithMany().HasForeignKey(s => s.ImportedByUserId);
        });

        // ScheduleOrder
        modelBuilder.Entity<ScheduleOrder>(e =>
        {
            e.ToTable("schedule_orders");
            e.HasKey(so => so.Id);
            e.Property(so => so.OrderNumber).HasMaxLength(100).IsRequired();
            e.Property(so => so.Qty).HasColumnName("qty").HasDefaultValue(1);
            e.HasOne(so => so.Schedule).WithMany(s => s.ScheduleOrders).HasForeignKey(so => so.ScheduleId);
        });

        // PartLineItem
        modelBuilder.Entity<PartLineItem>(e =>
        {
            e.ToTable("part_line_items");
            e.HasKey(p => p.Id);
            e.Property(p => p.PartNumber).HasMaxLength(100).IsRequired();
            e.Property(p => p.Title).HasMaxLength(255);
            e.Property(p => p.Description).HasMaxLength(1000);
            e.Property(p => p.Category).HasMaxLength(100);
            e.Property(p => p.Material).HasMaxLength(100);
            e.Property(p => p.Thickness).HasMaxLength(50);
            e.Property(p => p.StructCode).HasMaxLength(200);
            e.Property(p => p.Operations).HasMaxLength(500);
            e.Property(p => p.Notes).HasMaxLength(2000);
            e.HasOne(p => p.BatchProduct).WithMany(bp => bp.Parts).HasForeignKey(p => p.BatchProductId).IsRequired(false);
            e.HasOne(p => p.ScheduleOrder).WithMany(so => so.Parts).HasForeignKey(p => p.ScheduleOrderId).IsRequired(false);
        });

        // NestBatch
        modelBuilder.Entity<NestBatch>(e =>
        {
            e.ToTable("nest_batches");
            e.HasKey(nb => nb.Id);
            e.HasOne(nb => nb.Batch).WithMany().HasForeignKey(nb => nb.BatchId);
            e.HasOne(nb => nb.Plant).WithMany(p => p.NestBatches).HasForeignKey(nb => nb.PlantId);
        });

        // NestOrder
        modelBuilder.Entity<NestOrder>(e =>
        {
            e.ToTable("nest_orders");
            e.HasKey(o => o.Id);
            e.HasOne(o => o.ScheduleOrder).WithMany().HasForeignKey(o => o.ScheduleOrderId);
            e.HasOne(o => o.Plant).WithMany(p => p.NestOrders).HasForeignKey(o => o.PlantId);
        });

        // OrderItem
        modelBuilder.Entity<OrderItem>(e =>
        {
            e.ToTable("order_items");
            e.HasKey(i => i.Id);
            e.Property(i => i.Notes).HasMaxLength(1000);
            e.HasOne(i => i.NestOrder).WithMany(o => o.Items).HasForeignKey(i => i.NestOrderId);
            e.HasOne(i => i.Part).WithMany(p => p.OrderItems).HasForeignKey(i => i.PartId);
        });

        // BatchItem
        modelBuilder.Entity<BatchItem>(e =>
        {
            e.ToTable("batch_items");
            e.HasKey(i => i.Id);
            e.Property(i => i.Notes).HasMaxLength(1000);
            e.HasOne(i => i.NestBatch).WithMany(nb => nb.Items).HasForeignKey(i => i.NestBatchId);
            e.HasOne(i => i.Part).WithMany().HasForeignKey(i => i.PartId);
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
            e.HasOne(np => np.OrderItem).WithMany(oi => oi.NestedParts).HasForeignKey(np => np.OrderItemId).IsRequired(false);
            e.HasOne(np => np.BatchItem).WithMany(bi => bi.NestedParts).HasForeignKey(np => np.BatchItemId).IsRequired(false);
        });

        // RadanIdAssignment
        modelBuilder.Entity<RadanIdAssignment>(e =>
        {
            e.ToTable("radan_id_assignments");
            e.HasKey(r => r.Id);
            e.HasOne(r => r.OrderItem).WithOne(oi => oi.RadanIdAssignment).HasForeignKey<RadanIdAssignment>(r => r.OrderItemId).IsRequired(false);
            e.HasOne(r => r.BatchItem).WithOne(bi => bi.RadanIdAssignment).HasForeignKey<RadanIdAssignment>(r => r.BatchItemId).IsRequired(false);
            e.HasOne(r => r.Plant).WithMany(p => p.RadanIdAssignments).HasForeignKey(r => r.PlantId);
            e.HasIndex(r => new { r.RadanIdNumber, r.PlantId }).IsUnique();
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

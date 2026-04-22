using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace VisionCare.DataAccessLayer.Models;

public partial class VisionCareContext : DbContext
{
    public VisionCareContext()
    {
    }

    public VisionCareContext(DbContextOptions<VisionCareContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<Prescription> Prescriptions { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductVariant> ProductVariants { get; set; }

    public virtual DbSet<Promotion> Promotions { get; set; }

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<CartItem> CartItems { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<PrescriptionValidationRule> PrescriptionValidationRules { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<PreOrderCampaign> PreOrderCampaigns { get; set; }
    public virtual DbSet<PreOrderCampaignProduct> PreOrderCampaignProducts { get; set; }
    public virtual DbSet<PreOrderReservation> PreOrderReservations { get; set; }
    public virtual DbSet<Complaint> Complaints { get; set; }
    public virtual DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }
    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<ShippingMethod> ShippingMethods { get; set; }
    public virtual DbSet<ShippingOrder> ShippingOrders { get; set; }
    public virtual DbSet<ShippingStatus> ShippingStatuses { get; set; }
    public virtual DbSet<ShippingStatusHistory> ShippingStatusHistories { get; set; }

    public virtual DbSet<Warehouse> Warehouses { get; set; }
    public virtual DbSet<Inventory> Inventories { get; set; }
    public virtual DbSet<StockMovement> StockMovements { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<GoodsReceipt> GoodsReceipts { get; set; }
    public virtual DbSet<GoodsReceiptDetail> GoodsReceiptDetails { get; set; }

    public virtual DbSet<SystemSetting> SystemSettings { get; set; }
    public virtual DbSet<AuditLog> AuditLogs { get; set; }
    public virtual DbSet<BlacklistedIp> BlacklistedIps { get; set; }
    public virtual DbSet<CmsPage> CmsPages { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Categori__19093A0B7D56388C");

            entity.Property(e => e.CategoryName).HasMaxLength(100);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Orders__C3905BCFB2EF21E9");

            entity.Property(e => e.OrderDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.OrderStatus).HasMaxLength(50);
            entity.Property(e => e.OrderType).HasMaxLength(50);
            entity.Property(e => e.PaymentStatus).HasMaxLength(50);
            entity.Property(e => e.PaidAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ShippingFee).HasColumnType("decimal(18, 2)").HasDefaultValue(0m);
            entity.Property(e => e.TrackingNumber)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.Customer).WithMany(p => p.Orders)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK__Orders__Customer__3C69FB99");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.OrderItemId).HasName("PK__OrderIte__57ED0681BFF40293");

            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__OrderItem__Order__403A8C7D");

            entity.HasOne(d => d.Prescription).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.PrescriptionId)
                .HasConstraintName("FK__OrderItem__Presc__4222D4EF");

            entity.HasOne(d => d.Variant).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.VariantId)
                .HasConstraintName("FK__OrderItem__Varia__412EB0B6");

            entity.HasOne(d => d.AssignedLensMaker).WithMany(p => p.AssignedLensOrders)
                .HasForeignKey(d => d.AssignedLensMakerId)
                .HasConstraintName("FK__OrderItem__LensMk__7B5A6A1C");
        });

        modelBuilder.Entity<Prescription>(entity =>
        {
            entity.HasKey(e => e.PrescriptionId).HasName("PK__Prescrip__40130832C2EF04F6");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.OdAxis).HasColumnName("OD_Axis");
            entity.Property(e => e.OdCylinder)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("OD_Cylinder");
            entity.Property(e => e.OdSphere)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("OD_Sphere");
            entity.Property(e => e.OsAxis).HasColumnName("OS_Axis");
            entity.Property(e => e.OsCylinder)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("OS_Cylinder");
            entity.Property(e => e.OsSphere)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("OS_Sphere");
            entity.Property(e => e.Pd)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("PD");

            entity.HasOne(d => d.Customer).WithMany(p => p.Prescriptions)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK__Prescript__Custo__38996AB5");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__Products__B40CC6CDDFBEB52E");

            entity.Property(e => e.BasePrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Brand).HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Image2D).IsUnicode(false);
            entity.Property(e => e.IsPreOrder).HasDefaultValue(false);
            entity.Property(e => e.Model3D).IsUnicode(false);
            entity.Property(e => e.ProductName).HasMaxLength(200);

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK__Products__Catego__2E1BDC42");
        });

        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.HasKey(e => e.VariantId).HasName("PK__ProductV__0EA23384418E1F2D");

            entity.HasIndex(e => e.Sku, "UQ__ProductV__CA1ECF0D6BE4843F").IsUnique();

            entity.Property(e => e.AdditionalPrice)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Color).HasMaxLength(50);
            entity.Property(e => e.Size).HasMaxLength(50);
            entity.Property(e => e.Sku)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("SKU");
            entity.Property(e => e.StockQuantity).HasDefaultValue(0);

            entity.HasOne(d => d.Product).WithMany(p => p.ProductVariants)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__ProductVa__Produ__33D4B598");
        });

        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.HasKey(e => e.PromotionId).HasName("PK__Promotio__52C42FCF321A0F73");

            entity.HasIndex(e => e.PromoCode, "UQ__Promotio__32DBED35CC90BF06").IsUnique();

            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PromoCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.StartDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE1A58C1F139");

            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4CA9E46CDB");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D10534B550BD51").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PasswordHash).IsUnicode(false);
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK__Users__RoleId__276EDEB3");
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.CartId).HasName("PK__Carts__4154589F1A3E39C5");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Customer).WithMany(p => p.Carts)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK__Carts__Customer__5CDB2197");
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => e.CartItemId).HasName("PK__CartItem__4F27B0B5A7BE0F7E");

            entity.Property(e => e.Quantity).HasDefaultValue(1);

            entity.HasOne(d => d.Cart).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.CartId)
                .HasConstraintName("FK__CartItems__Cart__5E8BC0B9");

            entity.HasOne(d => d.Variant).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.VariantId)
                .HasConstraintName("FK__CartItems__Varian__5F7E9F5F");

            entity.HasOne(d => d.Prescription).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.PrescriptionId)
                .HasConstraintName("FK__CartItems__Prescr__60722E0F");
        });

        modelBuilder.Entity<OrderStatusHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId).HasName("PK__OrderStat__FD25C5D9E3F5A3B6");

            entity.Property(e => e.FromStatus).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ToStatus).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Note).HasColumnType("nvarchar(max)");
            entity.Property(e => e.ChangedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()")
                .HasColumnType("datetime2");

            entity.HasOne(e => e.Order)
                .WithMany(o => o.OrderStatusHistories)
                .HasForeignKey(e => e.OrderId)
                .HasConstraintName("FK__OrderStatus__Order__7C7BAF4E");

            entity.HasOne(e => e.ChangedByUser)
                .WithMany(u => u.OrderStatusHistories)
                .HasForeignKey(e => e.ChangedBy)
                .HasConstraintName("FK__OrderStatus__User__7D6B9B87");
        });


        modelBuilder.Entity<PrescriptionValidationRule>(entity =>
        {
            entity.HasKey(e => e.RuleId).HasName("PK__PrescriptValidationRule");
            entity.Property(e => e.RuleName).HasMaxLength(100);
            entity.Property(e => e.RuleType).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MinValue).HasPrecision(10, 2);
            entity.Property(e => e.MaxValue).HasPrecision(10, 2);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
            entity.Property(e => e.SortOrder).HasDefaultValue(0);
        });

        modelBuilder.Entity<PreOrderCampaign>(entity =>
        {
            entity.HasKey(e => e.CampaignId);
            entity.Property(e => e.CampaignCode).HasMaxLength(50);
            entity.Property(e => e.CampaignName).HasMaxLength(200);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.DiscountAmount).HasPrecision(18, 2);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");

            // Deposit configuration
            entity.Property(e => e.DepositRatio).HasPrecision(5, 4); // e.g., 0.3000 = 30%
            entity.Property(e => e.MinDepositAmount).HasPrecision(18, 0); // VND, no decimals
        });

        modelBuilder.Entity<PreOrderCampaignProduct>(entity =>
        {
            entity.HasKey(e => e.CampaignProductId);
            entity.Property(e => e.CampaignPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
        });

        modelBuilder.Entity<PreOrderReservation>(entity =>
        {
            entity.HasKey(e => e.ReservationId);
            entity.Property(e => e.ReservationCode).HasMaxLength(50);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.ShippingAddress).HasColumnType("nvarchar(max)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
        });

        modelBuilder.Entity<Complaint>(entity =>
        {
            entity.HasKey(e => e.ComplaintId);
            entity.Property(e => e.ComplaintType).HasMaxLength(100);
            entity.Property(e => e.Subject).HasMaxLength(200);
            entity.Property(e => e.ComplaintStatus).HasMaxLength(50);
            entity.Property(e => e.Priority).HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");

            entity.HasOne(d => d.Order).WithMany(p => p.Complaints)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(d => d.Customer).WithMany()
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(d => d.AssignedToUser).WithMany()
                .HasForeignKey(d => d.AssignedTo)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(d => d.ProcessedByUser).WithMany()
                .HasForeignKey(d => d.ProcessedBy)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(d => d.ResolvedByUser).WithMany()
                .HasForeignKey(d => d.ResolvedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ShippingMethod>(entity =>
        {
            entity.HasKey(e => e.ShippingMethodId);
            entity.Property(e => e.MethodCode).HasMaxLength(20).IsRequired();
            entity.Property(e => e.MethodName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Provider).HasMaxLength(50).IsRequired();
            entity.Property(e => e.BaseFee).HasColumnType("decimal(18,2)");
            entity.Property(e => e.FeePerKg).HasColumnType("decimal(18,2)");
            entity.Property(e => e.FreeShippingThreshold).HasColumnType("decimal(18,2)");
            entity.Property(e => e.MaxCodAmount).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<ShippingOrder>(entity =>
        {
            entity.HasKey(e => e.ShippingOrderId);
            entity.Property(e => e.ShippingOrderCode).HasMaxLength(50).IsRequired();
            entity.Property(e => e.CarrierTrackingNo).HasMaxLength(100);
            entity.Property(e => e.CarrierOrderNo).HasMaxLength(100);
            entity.Property(e => e.RecipientName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PhoneNumber).HasMaxLength(20).IsRequired();
            entity.Property(e => e.ProvinceCode).HasMaxLength(20);
            entity.Property(e => e.DistrictCode).HasMaxLength(20);
            entity.Property(e => e.WardCode).HasMaxLength(20);
            entity.Property(e => e.StreetAddress).HasMaxLength(500);
            entity.Property(e => e.ShippingFee).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CodFee).HasColumnType("decimal(18,2)");
            entity.Property(e => e.InsuranceFee).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalShippingCost).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

            entity.HasOne(e => e.Order)
                .WithMany(o => o.ShippingOrders)
                .HasForeignKey(e => e.OrderId)
                .HasConstraintName("FK__ShippingOrder__Order");

            entity.HasOne(e => e.ShippingMethod)
                .WithMany(s => s.ShippingOrders)
                .HasForeignKey(e => e.ShippingMethodId)
                .HasConstraintName("FK__ShippingOrder__Method");
        });

        modelBuilder.Entity<ShippingStatus>(entity =>
        {
            entity.HasKey(e => e.ShippingStatusId);
            entity.Property(e => e.StatusCode).HasMaxLength(50).IsRequired();
            entity.Property(e => e.StatusName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.StatusOrder).HasDefaultValue(0);
        });

        modelBuilder.Entity<ShippingStatusHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId);
            entity.Property(e => e.CarrierStatusText).HasMaxLength(200);
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

            entity.HasOne(e => e.ShippingOrder)
                .WithMany(s => s.StatusHistories)
                .HasForeignKey(e => e.ShippingOrderId)
                .HasConstraintName("FK__ShippingStatusHistory__ShippingOrder");

            entity.HasOne(e => e.FromStatus)
                .WithMany()
                .HasForeignKey(e => e.FromStatusId)
                .HasConstraintName("FK__ShippingStatusHistory__FromStatus");

            entity.HasOne(e => e.ToStatus)
                .WithMany()
                .HasForeignKey(e => e.ToStatusId)
                .HasConstraintName("FK__ShippingStatusHistory__ToStatus");
        });

        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.HasKey(e => e.WarehouseId);
            entity.Property(e => e.WarehouseCode).HasMaxLength(20).IsRequired();
            entity.Property(e => e.WarehouseName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.WarehouseType).HasMaxLength(20);
            entity.Property(e => e.ProvinceCode).HasMaxLength(20);
            entity.Property(e => e.ProvinceName).HasMaxLength(100);
            entity.Property(e => e.DistrictCode).HasMaxLength(20);
            entity.Property(e => e.DistrictName).HasMaxLength(100);
            entity.Property(e => e.WardCode).HasMaxLength(20);
            entity.Property(e => e.WardName).HasMaxLength(100);
            entity.Property(e => e.StreetAddress).HasMaxLength(500);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.HasKey(e => e.InventoryId);
            entity.Property(e => e.BatchNumber).HasMaxLength(50);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

            entity.HasOne(e => e.Variant)
                .WithMany(v => v.Inventories)
                .HasForeignKey(e => e.VariantId)
                .HasConstraintName("FK__Inventory__Variant");

            entity.HasOne(e => e.Warehouse)
                .WithMany(w => w.Inventories)
                .HasForeignKey(e => e.WarehouseId)
                .HasConstraintName("FK__Inventory__Warehouse");

            entity.HasIndex(e => new { e.VariantId, e.WarehouseId }).IsUnique();
        });

        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.HasKey(e => e.MovementId);
            entity.Property(e => e.MovementType).HasMaxLength(30).IsRequired();
            entity.Property(e => e.ReferenceType).HasMaxLength(30);
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.StaffNote).HasMaxLength(1000);
            entity.Property(e => e.PerformedAt).HasDefaultValueSql("SYSUTCDATETIME()");

            entity.HasOne(e => e.Variant)
                .WithMany()
                .HasForeignKey(e => e.VariantId)
                .HasConstraintName("FK__StockMovement__Variant");

            entity.HasOne(e => e.Warehouse)
                .WithMany()
                .HasForeignKey(e => e.WarehouseId)
                .HasConstraintName("FK__StockMovement__Warehouse");

            entity.HasOne(e => e.PerformedByUser)
                .WithMany(u => u.StockMovements)
                .HasForeignKey(e => e.PerformedBy)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.SupplierId);
            entity.Property(e => e.SupplierCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.SupplierName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<GoodsReceipt>(entity =>
        {
            entity.HasKey(e => e.GoodsReceiptId);
            entity.Property(e => e.ReceiptNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("draft");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            
            entity.HasOne(d => d.Campaign)
                .WithMany()
                .HasForeignKey(d => d.CampaignId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasOne(d => d.CreatedByUser)
                .WithMany()
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(d => d.ManagerUser)
                .WithMany()
                .HasForeignKey(d => d.ManagerId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasOne(d => d.Warehouse)
                .WithMany()
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GoodsReceiptDetail>(entity =>
        {
            entity.HasKey(e => e.DetailId);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
            
            entity.HasOne(d => d.GoodsReceipt)
                .WithMany(p => p.Details)
                .HasForeignKey(d => d.GoodsReceiptId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(d => d.Variant)
                .WithMany()
                .HasForeignKey(d => d.VariantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Message).HasMaxLength(1000);
            entity.Property(e => e.Type).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            
            entity.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.HasKey(e => e.SettingId);
            entity.Property(e => e.SettingKey).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.SettingKey).IsUnique();
            entity.Property(e => e.GroupName).HasMaxLength(50).HasDefaultValue("General");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.AuditId);
            entity.Property(e => e.Action).HasMaxLength(100).IsRequired();
            entity.Property(e => e.EntityName).HasMaxLength(100);
            entity.Property(e => e.IpAddress).HasMaxLength(50);
            
            entity.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<BlacklistedIp>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.IpAddress).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.IpAddress).IsUnique();
        });

        modelBuilder.Entity<CmsPage>(entity =>
        {
            entity.HasKey(e => e.PageId);
            entity.Property(e => e.Slug).HasMaxLength(200).IsRequired();
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

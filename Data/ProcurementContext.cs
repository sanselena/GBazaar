using Microsoft.EntityFrameworkCore;
using GBazaar.Models;
using GBazaar.Models.Enums;

namespace Gbazaar.Data
{
    public class ProcurementContext : DbContext
    {
        public ProcurementContext(DbContextOptions<ProcurementContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<PaymentTerm> PaymentTerms { get; set; }
        public DbSet<PurchaseRequest> PurchaseRequests { get; set; }
        public DbSet<PRItem> PRItems { get; set; }
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<POItem> POItems { get; set; }
        public DbSet<Budget> Budgets { get; set; }
        public DbSet<ApprovalRule> ApprovalRules { get; set; }
        public DbSet<ApprovalHistory> ApprovalHistories { get; set; }
        public DbSet<GoodsReceipt> GoodsReceipts { get; set; }
        public DbSet<GoodsReceiptItem> GoodsReceiptItems { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<SupplierRating> SupplierRatings { get; set; }
      public DbSet<Attachment> Attachments { get; set; }
      public DbSet<Product> Products { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>( entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.CreatedAt)
                       .HasDefaultValueSql("GETUTCDATE()");
                entity.HasOne(u => u.Role)
                      .WithMany(r => r.Users)
                      .HasForeignKey(u => u.RoleID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(u => u.Department)
                      .WithMany(d => d.Users)
                      .HasForeignKey(u => u.DepartmentID)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Department>(entity =>
            {
                entity.HasIndex(d => d.BudgetCode).IsUnique(); 
                entity.HasOne(d => d.Manager)
                      .WithMany()
                      .HasForeignKey(d => d.ManagerID)
                      .OnDelete(DeleteBehavior.SetNull);
                entity.HasMany(d => d.Budgets)
                      .WithOne(b => b.Department)
                      .HasForeignKey(b => b.DepartmentID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasMany(d => d.Users)
                      .WithOne(u => u.Department)
                      .HasForeignKey(u => u.DepartmentID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Role>(entity =>
            { 
                entity.HasIndex(r => r.RoleName).IsUnique();
                entity.HasMany(r => r.RolePermissions)
                      .WithOne(rp => rp.Role)
                      .HasForeignKey(rp => rp.RoleID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasMany(r => r.Users)
                      .WithOne(u => u.Role)
                      .HasForeignKey(u => u.RoleID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasMany(r => r.ApprovalRules)
                      .WithOne(ar => ar.RequiredRole)
                      .HasForeignKey(ar => ar.RequiredRoleID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Supplier>(entity =>
            { 
                entity.HasIndex(s => s.SupplierName).IsUnique();
                entity.HasIndex(s => s.ContactInfo).IsUnique();
                entity.HasOne(s => s.PaymentTerm)
                      .WithMany(pt => pt.Suppliers)
                      .HasForeignKey(s => s.PaymentTermID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasMany(s => s.PurchaseOrders)
                      .WithOne(po => po.Supplier)
                      .HasForeignKey(po => po.SupplierID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasMany(s => s.Invoices)
                      .WithOne(i => i.Supplier)
                      .HasForeignKey(i => i.SupplierID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasMany(s => s.PRItems)
                      .WithOne(i => i.Supplier)
                      .HasForeignKey(i => i.SupplierID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasMany(s => s.PurchaseRequests) 
                      .WithOne(pr => pr.Supplier)
                      .HasForeignKey(pr => pr.SupplierID)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Budget>(entity =>
            {
                entity.HasIndex(b => new {b.DepartmentID, b.FiscalYear }).IsUnique();
                entity.HasOne(b => b.Department)
                      .WithMany(d => d.Budgets)
                      .HasForeignKey(b => b.DepartmentID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PaymentTerm>(entity =>
            { 
                entity.HasIndex(pt => pt.Description).IsUnique();
                entity.HasMany(pt => pt.Suppliers)
                      .WithOne(s => s.PaymentTerm)
                      .HasForeignKey(s => s.PaymentTermID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PRItem>(entity =>
            {
                entity.HasOne(i => i.PurchaseRequest)
                      .WithMany(pr => pr.PRItems)
                      .HasForeignKey(i => i.PRID)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(i => i.Supplier)
                      .WithMany(s => s.PRItems)
                      .HasForeignKey(i => i.SupplierID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(i => i.Product)
                      .WithMany(p => p.PRItems)
                      .HasForeignKey(i => i.ProductID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.HasIndex(i => new { i.InvoiceNumber, i.SupplierID }).IsUnique();
                entity.HasOne(i => i.Supplier)
                      .WithMany(s => s.Invoices)
                      .HasForeignKey(i => i.SupplierID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(i => i.PurchaseOrder)
                      .WithMany(po => po.Invoices)
                      .HasForeignKey(i => i.POID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.Property(i => i.PaymentStatus)
                      .HasConversion<int?>();          
            });

            modelBuilder.Entity<PurchaseRequest>(entity =>
            {
                entity.HasOne(pr => pr.Requester)
                      .WithMany(u => u.PurchaseRequests)
                      .HasForeignKey(pr => pr.RequesterID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasMany(pr => pr.PRItems)
                      .WithOne(i => i.PurchaseRequest)
                      .HasForeignKey(i => i.PRID)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasMany(pr => pr.ApprovalHistories)
                      .WithOne(ah => ah.PurchaseRequest)
                      .HasForeignKey(ah => ah.PRID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(pr => pr.PurchaseOrder)
                      .WithOne(po => po.PurchaseRequest)
                      .HasForeignKey<PurchaseOrder>(po => po.PRID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.Property(pr => pr.PRStatus)
                      .HasConversion<int>();
                entity.HasOne(pr => pr.Supplier)
                      .WithMany(s => s.PurchaseRequests)
                      .HasForeignKey(pr => pr.SupplierID)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<PurchaseOrder>(entity =>
            {
                entity.HasOne(po => po.PurchaseRequest)
                      .WithOne(pr => pr.PurchaseOrder)
                      .HasForeignKey<PurchaseOrder>(po => po.PRID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasMany(po => po.POItems)
                      .WithOne(i => i.PurchaseOrder)
                      .HasForeignKey(i => i.POID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(po => po.Supplier)
                      .WithMany(s => s.PurchaseOrders)
                      .HasForeignKey(po => po.SupplierID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasMany(po => po.GoodsReceipts)
                      .WithOne(gr => gr.PurchaseOrder)
                      .HasForeignKey(gr => gr.POID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasMany(po => po.Invoices)
                      .WithOne(i => i.PurchaseOrder)
                      .HasForeignKey(i => i.POID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasMany(po => po.SupplierRatings)
                      .WithOne(sr => sr.PurchaseOrder)
                      .HasForeignKey(sr => sr.POID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.Property(po => po.POStatus)
                      .HasConversion<int>();
            });

            modelBuilder.Entity<SupplierRating>(entity =>
            {
                entity.HasOne(sr => sr.PurchaseOrder)
                      .WithMany(po => po.SupplierRatings)
                      .HasForeignKey(sr => sr.POID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(sr => sr.Rater)
                      .WithMany(u => u.SupplierRatings)
                      .HasForeignKey(sr => sr.RatedByUserID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.Property(sr => sr.RatedOn)
                      .HasDefaultValueSql("GETUTCDATE()");
            });

            modelBuilder.Entity<GoodsReceipt>(entity =>
            {
                entity.HasOne(gr => gr.PurchaseOrder)
                      .WithMany(po => po.GoodsReceipts)
                      .HasForeignKey(gr => gr.POID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasMany(gr => gr.GoodsReceiptItems)
                      .WithOne(gri => gri.GoodsReceipt)
                      .HasForeignKey(gri => gri.GRID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(gr => gr.Receiver)
                      .WithMany(u => u.GoodsReceipts)
                      .HasForeignKey(gr => gr.ReceivedByUserID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.Property(gr => gr.DateReceived)
                      .HasDefaultValueSql("GETUTCDATE()");
            });

            modelBuilder.Entity<ApprovalRule>(entity =>
            {
                entity.HasIndex(ar => new {ar.MinAmount, ar.MaxAmount ,ar.RequiredRoleID, ar.ApprovalLevel}).IsUnique();
                entity.HasOne(ar => ar.RequiredRole)
                      .WithMany(r => r.ApprovalRules)
                      .HasForeignKey(ar => ar.RequiredRoleID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ApprovalHistory>(entity =>
            {
                entity.HasOne(ah => ah.PurchaseRequest)
                      .WithMany(pr => pr.ApprovalHistories)
                      .HasForeignKey(ah => ah.PRID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(ah => ah.Approver)
                      .WithMany(u => u.ApprovalHistories)
                      .HasForeignKey(ah => ah.ApproverID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.Property(ah => ah.ActionDate)
                      .HasDefaultValueSql("GETUTCDATE()");
                entity.Property(ah => ah.ActionType)
                      .HasConversion<int>();
            });

            modelBuilder.Entity<Attachment>(entity =>
            {
                entity.HasIndex(a => new { a.EntityType, a.EntityID, a.FileName }).IsUnique();
                entity.Property(a => a.UploadDate)
                      .HasDefaultValueSql("GETUTCDATE()");
                entity.Property(a => a.EntityType)
                      .HasConversion<int>();
                entity.HasOne(a => a.Uploader)
                      .WithMany(u => u.Attachments)
                      .HasForeignKey(a => a.UploadedByUserID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<POItem>(entity =>
            {
                entity.HasOne(i => i.PurchaseOrder)
                      .WithMany(po => po.POItems)
                      .HasForeignKey(i => i.POID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasOne(p => p.Supplier)
                      .WithMany(s => s.Products)
                      .HasForeignKey(p => p.SupplierID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(p => new { p.SupplierID, p.ProductName }).IsUnique();
                entity.HasMany(p => p.PRItems)
                      .WithOne(pri => pri.Product)
                      .HasForeignKey(p => p.ProductID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<GoodsReceiptItem>(entity =>
            {
                entity.HasOne(gri => gri.GoodsReceipt)
                      .WithMany(gr => gr.GoodsReceiptItems)
                      .HasForeignKey(gri => gri.GRID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(gri => gri.POItem)
                      .WithMany()
                      .HasForeignKey(gri => gri.POItemID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Permission>(entity =>
            {
                entity.HasIndex(p => p.PermissionName).IsUnique();
                entity.HasMany(p => p.RolePermissions)
                      .WithOne(rp => rp.Permission)
                      .HasForeignKey(rp => rp.PermissionID)
                      .OnDelete(DeleteBehavior.Restrict);
            });
            
            modelBuilder.Entity<RolePermission>(entity =>
            {
                entity.HasKey(rp => new { rp.RoleID, rp.PermissionID });
                entity.HasOne(rp => rp.Role)
                      .WithMany(r => r.RolePermissions)
                      .HasForeignKey(rp => rp.RoleID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(rp => rp.Permission)
                      .WithMany(p => p.RolePermissions)
                      .HasForeignKey(rp => rp.PermissionID)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
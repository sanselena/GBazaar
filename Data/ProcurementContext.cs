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

        // DbSet properties for all your models
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- Enum Conversions ---
            modelBuilder.Entity<PurchaseRequest>()
                .Property(pr => pr.PRStatus)
                .HasConversion<string>();

            modelBuilder.Entity<PurchaseOrder>()
                .Property(po => po.POStatus)
                .HasConversion<string>();

            modelBuilder.Entity<Invoice>()
                .Property(i => i.PaymentStatus)
                .HasConversion<string>();

            modelBuilder.Entity<ApprovalHistory>()
                .Property(ah => ah.ActionType)
                .HasConversion<string>();

            modelBuilder.Entity<Attachment>()
                .Property(a => a.EntityType)
                .HasConversion<string>();

            // --- Entity Configurations ---

            // User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasOne(u => u.Role)
                      .WithMany(r => r.Users)
                      .HasForeignKey(u => u.RoleID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(u => u.Department)
                      .WithMany(d => d.Users)
                      .HasForeignKey(u => u.DepartmentID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Role
            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasIndex(r => r.RoleName).IsUnique();
            });

            // Permission
            modelBuilder.Entity<Permission>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.HasIndex(p => p.Name).IsUnique();
            });

            // RolePermission (Many-to-Many)
            modelBuilder.Entity<RolePermission>(entity =>
            {
                // Corrected composite key to match the model (RoleID and PermissionId)
                entity.HasKey(rp => new { rp.RoleID, rp.PermissionId }); 
                entity.HasOne(rp => rp.Role)
                      .WithMany(r => r.RolePermissions)
                      .HasForeignKey(rp => rp.RoleID);
                entity.HasOne(rp => rp.Permission)
                      .WithMany(p => p.RolePermissions)
                      .HasForeignKey(rp => rp.PermissionId); // Corrected FK to match the model
            });

            // Department
            modelBuilder.Entity<Department>(entity =>
            {
                entity.HasIndex(d => d.DepartmentName).IsUnique();
                entity.HasIndex(d => d.BudgetCode).IsUnique();
                entity.HasOne(d => d.Budget)
                      .WithOne(b => b.Department)
                      .HasForeignKey<Budget>(b => b.DepartmentID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Supplier
            modelBuilder.Entity<Supplier>(entity =>
            {
                entity.HasIndex(s => s.SupplierName).IsUnique();
                entity.HasIndex(s => s.TaxID).IsUnique();
                entity.HasOne(s => s.PaymentTerm)
                      .WithMany(pt => pt.Suppliers)
                      .HasForeignKey(s => s.PaymentTermID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // PaymentTerm
            modelBuilder.Entity<PaymentTerm>(entity =>
            {
                entity.HasIndex(pt => pt.Description).IsUnique();
            });

            // PurchaseRequest
            modelBuilder.Entity<PurchaseRequest>(entity =>
            {
                entity.HasOne(pr => pr.Requester)
                      .WithMany(u => u.PurchaseRequests)
                      .HasForeignKey(pr => pr.RequesterID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasMany(pr => pr.PRItems)
                      .WithOne(item => item.PurchaseRequest)
                      .HasForeignKey(item => item.PRID)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // PRItem
            modelBuilder.Entity<PRItem>(entity =>
            {
                entity.HasKey(e => e.PRItemID);
            });

            // PurchaseOrder
            modelBuilder.Entity<PurchaseOrder>(entity =>
            {
                entity.HasIndex(po => po.PRID).IsUnique();
                entity.HasOne(po => po.Supplier)
                      .WithMany(s => s.PurchaseOrders)
                      .HasForeignKey(po => po.SupplierID)
                      .OnDelete(DeleteBehavior.Restrict);
                // REMOVED: HasMany for POItems because the navigation property does not exist on PurchaseOrder model.
                // The relationship is configured from the POItem side.
                entity.HasOne(po => po.PurchaseRequest)
                      .WithOne(pr => pr.PurchaseOrder)
                      .HasForeignKey<PurchaseOrder>(po => po.PRID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // POItem
            modelBuilder.Entity<POItem>(entity =>
            {
                entity.HasKey(poi => poi.POItemID);
                entity.HasOne(poi => poi.PurchaseOrder) // Added this relationship from the "many" side
                      .WithMany() // No navigation property on PurchaseOrder, so WithMany() is empty
                      .HasForeignKey(poi => poi.POID);
            });

            // Budget
            modelBuilder.Entity<Budget>(entity =>
            {
                entity.HasKey(b => b.BudgetID);
            });

            // ApprovalRule
            modelBuilder.Entity<ApprovalRule>(entity =>
            {
                entity.HasOne(ar => ar.RequiredRole)
                      .WithMany()
                      .HasForeignKey(ar => ar.RequiredRoleID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ApprovalHistory
            modelBuilder.Entity<ApprovalHistory>(entity =>
            {
                entity.HasOne(ah => ah.PurchaseRequest)
                      .WithMany(pr => pr.ApprovalHistories)
                      .HasForeignKey(ah => ah.PRID)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(ah => ah.Approver)
                      .WithMany()
                      .HasForeignKey(ah => ah.ApproverID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // GoodsReceipt
            modelBuilder.Entity<GoodsReceipt>(entity =>
            {
                entity.HasOne(gr => gr.PurchaseOrder)
                      .WithMany(po => po.GoodsReceipts)
                      .HasForeignKey(gr => gr.POID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(gr => gr.Receiver)
                      .WithMany()
                      .HasForeignKey(gr => gr.ReceivedByUserID)
                      .OnDelete(DeleteBehavior.Restrict);
                // REMOVED: HasMany for GoodsReceiptItems because the navigation property does not exist on GoodsReceipt model.
                // The relationship is configured from the GoodsReceiptItem side.
            });

            // GoodsReceiptItem
            modelBuilder.Entity<GoodsReceiptItem>(entity =>
            {
                entity.HasKey(gri => gri.GRItemID);
                entity.HasOne(gri => gri.GoodsReceipt) // Added this relationship from the "many" side
                      .WithMany() // No navigation property on GoodsReceipt, so WithMany() is empty
                      .HasForeignKey(gri => gri.GRID);
                entity.HasOne(gri => gri.POItem)
                      .WithMany()
                      .HasForeignKey(gri => gri.POItemID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Invoice
            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.HasIndex(inv => inv.InvoiceNumber).IsUnique();
                entity.HasOne(inv => inv.PurchaseOrder)
                      .WithMany(po => po.Invoices)
                      .HasForeignKey(inv => inv.POID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(inv => inv.Supplier)
                      .WithMany()
                      .HasForeignKey(inv => inv.SupplierID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // SupplierRating
            modelBuilder.Entity<SupplierRating>(entity =>
            {
                entity.HasOne(sr => sr.PurchaseOrder)
                      .WithMany(po => po.SupplierRatings)
                      .HasForeignKey(sr => sr.POID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(sr => sr.Rater)
                      .WithMany()
                      .HasForeignKey(sr => sr.RatedByUserID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Attachment
            modelBuilder.Entity<Attachment>(entity =>
            {
                entity.HasOne(a => a.Uploader)
                      .WithMany()
                      .HasForeignKey(a => a.UploadedByUserID)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
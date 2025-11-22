using Microsoft.EntityFrameworkCore;
using GBazaar.Models;
using System;
using System.Linq;

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
        public DbSet<Department> Departments { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<PaymentTerm> PaymentTerms { get; set; }    
        public DbSet<Category> Categories { get; set; }
        public DbSet<SubCategory> SubCategories { get; set; }
        public DbSet<PurchaseRequest> PurchaseRequests { get; set; }
        public DbSet<PRItem> PRItems { get; set; }
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<Budget> Budgets { get; set; }
        public DbSet<ApprovalRule> ApprovalRules { get; set; }
        public DbSet<ApprovalHistory> ApprovalHistories { get; set; }
        public DbSet<GoodsReceipt> GoodsReceipts { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<SupplierRating> SupplierRatings { get; set; }
        public DbSet<Attachment> Attachments { get; set; }
        public DbSet<PRStatus> PRStatuses { get; set; }
        public DbSet<POStatus> POStatuses { get; set; }
        public DbSet<PaymentStatus> PaymentStatuses { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.UserName).IsUnique();
                entity.HasOne(u => u.Role)
                      .WithMany(r => r.Users)
                      .HasForeignKey(u => u.RoleID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(u => u.Department)
                      .WithMany(d => d.Users)
                      .HasForeignKey(u => u.DepartmentID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasMany (u => u.PurchaseRequests)
                      .WithOne(pr => pr.Requester)
                      .HasForeignKey(pr => pr.RequesterID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasMany (u => u.ApprovalHistories)
                      .WithOne(ah => ah.Approver)
                      .HasForeignKey(ah => ah.ApproverID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasMany (u => u.GoodsReceipts)
                      .WithOne(gr => gr.Receiver)
                      .HasForeignKey(gr => gr.ReceivedByUserID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasMany (u => u.SupplierRatings)
                      .WithOne(sr => sr.Rater)
                      .HasForeignKey(sr => sr.RatedByUserID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasMany (u => u.Attachments)
                      .WithOne(a => a.Uploader)
                      .HasForeignKey(a => a.UploadedByUserID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasIndex(r => r.RoleName).IsUnique();
                entity.HasMany (r => r.Users)
                      .WithOne(u => u.Role)
                      .HasForeignKey(u => u.RoleID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Department>(entity =>
            {
                entity.HasIndex(d => d.DepartmentName).IsUnique();
                entity.HasIndex(d => d.BudgetCode).IsUnique();
                entity.HasMany (d => d.Users)
                      .WithOne(u => u.Department)
                      .HasForeignKey(u => u.DepartmentID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(d => d.Budget)
                      .WithOne(b => b.Department)
                      .HasForeignKey<Budget>(b => b.DepartmentID)
                      .OnDelete(DeleteBehavior.Restrict);

            });

            modelBuilder.Entity<Supplier>(entity =>
            {
                entity.HasIndex(s => s.SupplierName).IsUnique();
                entity.HasIndex(s => s.TaxID).IsUnique();
                entity.HasMany (s => s.PurchaseOrders)
                      .WithOne(po => po.Supplier)
                      .HasForeignKey(po => po.SupplierID)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(s => s.PaymentTerm)
                      .WithMany(pt => pt.Suppliers)
                      .HasForeignKey(s => s.PaymentTermsID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PaymentTerm>(entity =>
            {
                entity.HasIndex(pt => pt.Description).IsUnique();
                entity.HasMany(pt => pt.Suppliers)
                      .WithOne(s => s.PaymentTerm)
                      .HasForeignKey(s => s.PaymentTermsID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Category>(Entity =>
            {
                Entity.HasIndex(c => c.CategoryName).IsUnique();
                Entity.HasMany(c => c.SubCategories)
                      .WithOne(sc => sc.Category)
                      .HasForeignKey(sc => sc.CategoryID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<SubCategory>(Entity =>
            {
                Entity.HasIndex(sc => sc.SubCategoryName).IsUnique();
                Entity.HasOne(sc => sc.Category)
                      .WithMany(c => c.SubCategories)
                      .HasForeignKey(sc => sc.CategoryID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PurchaseRequest>(Entity =>
            {
                Entity.HasOne(pr => pr.Requester)
                      .WithMany(u => u.PurchaseRequests)
                      .HasForeignKey(pr => pr.RequesterID)
                      .OnDelete(DeleteBehavior.Restrict);
                Entity.HasOne(pr => pr.PRStatus)
                      .WithMany(s => s.PurchaseRequests)
                      .HasForeignKey(pr => pr.PRStatusID)
                      .OnDelete(DeleteBehavior.Restrict);
                Entity.HasMany(pr => pr.PRItems)
                      .WithOne(item => item.PurchaseRequest)
                      .HasForeignKey(item => item.PRID)
                      .OnDelete(DeleteBehavior.Cascade);
                Entity.HasOne(pr => pr.PurchaseOrder)
                      .WithOne(po => po.PurchaseRequest)
                      .HasForeignKey<PurchaseOrder>(po => po.PRID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PRItem>(Entity =>
            {
                Entity.HasOne(pri => pri.PurchaseRequest)
                      .WithMany(pr => pr.PRItems)
                      .HasForeignKey(pri => pri.PRID)
                      .OnDelete(DeleteBehavior.Cascade);
                Entity.HasOne(pri => pri.SubCategory)
                      .WithMany(sc => sc.PRItems)
                      .HasForeignKey(pri => pri.SubCategoryID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PurchaseOrder>(Entity =>
            {
                Entity.HasIndex(po => po.PRID).IsUnique();
                Entity.HasOne(po => po.PurchaseRequest)
                      .WithOne(pr => pr.PurchaseOrder)
                      .HasForeignKey<PurchaseOrder>(po => po.PRID)
                      .OnDelete(DeleteBehavior.Restrict);
                Entity.HasMany(po => po.GoodsReceipts)
                      .WithOne(gr => gr.PurchaseOrder)
                      .HasForeignKey(gr => gr.POID)
                      .OnDelete(DeleteBehavior.Restrict);
                Entity.HasMany(po => po.Invoices)
                      .WithOne(inv => inv.PurchaseOrder)
                      .HasForeignKey(inv => inv.POID)
                      .OnDelete(DeleteBehavior.Restrict);
                Entity.HasMany(po => po.SupplierRatings)
                      .WithOne(sr => sr.PurchaseOrder)
                      .HasForeignKey(sr => sr.POID)
                      .OnDelete(DeleteBehavior.Restrict);
                Entity.HasOne(Entity => Entity.Supplier)
                      .WithMany(s => s.PurchaseOrders)
                      .HasForeignKey(Entity => Entity.SupplierID)
                      .OnDelete(DeleteBehavior.Restrict); 
                Entity.HasOne(Entity => Entity.POStatus)        
                      .WithMany(s => s.PurchaseOrders)
                      .HasForeignKey(Entity => Entity.POStatusID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Budget>(Entity =>
            {
                Entity.HasOne(b => b.Department)
                      .WithOne(d => d.Budget)
                      .HasForeignKey<Budget>(b => b.DepartmentID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ApprovalRule>(Entity =>
            {
                Entity.HasOne(ar => ar.RequiredRole)
                      .WithMany(r => r.ApprovalRules)
                      .HasForeignKey(ar => ar.RequiredRoleID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ApprovalHistory>(Entity =>
            {
                Entity.HasOne(ah => ah.PurchaseRequest)
                      .WithMany(pr => pr.ApprovalHistories)
                      .HasForeignKey(ah => ah.PRID)
                      .OnDelete(DeleteBehavior.Cascade);
                Entity.HasOne(ah => ah.Approver)
                      .WithMany(u => u.ApprovalHistories)
                      .HasForeignKey(ah => ah.ApproverID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<GoodsReceipt>(Entity =>
            {
                Entity.HasOne(gr => gr.PurchaseOrder)
                      .WithMany(po => po.GoodsReceipts)
                      .HasForeignKey(gr => gr.POID)
                      .OnDelete(DeleteBehavior.Restrict);
                Entity.HasOne(gr => gr.Receiver)
                      .WithMany(u => u.GoodsReceipts)
                      .HasForeignKey(gr => gr.ReceivedByUserID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Invoice>(Entity =>
            {
                Entity.HasIndex(inv => inv.InvoiceNumber).IsUnique();
                Entity.HasOne(inv => inv.PurchaseOrder)
                      .WithMany(po => po.Invoices)
                      .HasForeignKey(inv => inv.POID)
                      .OnDelete(DeleteBehavior.Restrict);
                Entity.HasOne(inv => inv.PaymentStatus)
                      .WithMany(ps => ps.Invoices)
                      .HasForeignKey(inv => inv.PaymentStatusID)
                      .OnDelete(DeleteBehavior.Restrict);
                Entity.HasOne(inv => inv.Supplier)  
                      .WithMany(s => s.Invoices)
                      .HasForeignKey(inv => inv.SupplierID)
                      .OnDelete(DeleteBehavior.Restrict);
            });
            
            modelBuilder.Entity<SupplierRating>(Entity =>
            {
                Entity.HasOne(sr => sr.PurchaseOrder)
                      .WithMany(po => po.SupplierRatings)
                      .HasForeignKey(sr => sr.POID)
                      .OnDelete(DeleteBehavior.Restrict);
                Entity.HasOne(sr => sr.Rater)
                      .WithMany(u => u.SupplierRatings)
                      .HasForeignKey(sr => sr.RatedByUserID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Attachment>(Entity => 
            {
                Entity.HasOne(a => a.Uploader)
                      .WithMany(u => u.Attachments)
                      .HasForeignKey(a => a.UploadedByUserID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PRStatus>(Entity =>
            {
                Entity.HasIndex(s => s.StatusType).IsUnique();
                Entity.HasMany(s => s.PurchaseRequests)
                      .WithOne(pr => pr.PRStatus)
                      .HasForeignKey(pr => pr.PRStatusID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<POStatus>(Entity =>
            {
                Entity.HasIndex(s => s.StatusType).IsUnique();
                Entity.HasMany(s => s.PurchaseOrders)
                      .WithOne(po => po.POStatus)
                      .HasForeignKey(po => po.POStatusID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PaymentStatus>(Entity =>
            {
                Entity.HasIndex(s => s.StatusType).IsUnique();
                Entity.HasMany(s => s.Invoices)
                      .WithOne(inv => inv.PaymentStatus)
                      .HasForeignKey(inv => inv.PaymentStatusID)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }   
    }
}
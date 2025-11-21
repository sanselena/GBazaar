using Microsoft.EntityFrameworkCore;
using GBazaar.Models;
using System;
using System.Linq;

namespace GBazaar.Data
{
    public class ProcurementContext : DbContext
    {
        public ProcurementContext(DbContextOptions<ProcurementContext> options)
            : base(options)
        {
        }

        // ==========================================================
        // DbSet Properties: 20 Tablonun Hepsi
        // ==========================================================

        // Yönetim ve Kullanıcı
        public DbSet<User> Users { get; set; } = default!;
        public DbSet<Role> Roles { get; set; } = default!;
        public DbSet<Department> Departments { get; set; } = default!;
        public DbSet<Supplier> Suppliers { get; set; } = default!;

        // Satın Alma İşlemleri (P2P)
        public DbSet<PurchaseRequest> PurchaseRequests { get; set; } = default!;
        public DbSet<PRItem> PRItems { get; set; } = default!;
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; } = default!;
        public DbSet<Invoice> Invoices { get; set; } = default!;
        public DbSet<GoodsReceipt> GoodsReceipts { get; set; } = default!;

        // Kontrol, Denetim ve Onay
        public DbSet<Budget> Budgets { get; set; } = default!;
        public DbSet<ApprovalRule> ApprovalRules { get; set; } = default!;
        public DbSet<ApprovalHistory> ApprovalHistory { get; set; } = default!;
        public DbSet<SupplierRating> SupplierRatings { get; set; } = default!;

        // Referans, Statü ve Ekler Tabloları
        public DbSet<Category> Categories { get; set; } = default!;
        public DbSet<SubCategory> SubCategories { get; set; } = default!;
        public DbSet<PaymentTerm> PaymentTerms { get; set; } = default!;
        public DbSet<PRStatus> PRStatuses { get; set; } = default!;
        public DbSet<POStatus> POStatuses { get; set; } = default!;
        public DbSet<PaymentStatus> PaymentStatuses { get; set; } = default!;
        public DbSet<Attachment> Attachments { get; set; } = default!;

        // ==========================================================
        // OnModelCreating: İlişkiler ve Kısıtlamalar (Fluent API)
        // ==========================================================
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- 1. UNIQUE Kısıtlamaları ve Özel Indexler ---

            // SQL'de unique olarak tanımlandı: PRID (PurchaseOrders'da)
            modelBuilder.Entity<PurchaseOrder>()
                .HasIndex(po => po.PRID)
                .IsUnique();

            // SQL'de DepartmentID unique tanımlandı (tek bir bütçe kaydı olması için)
            // Ancak genellikle bütçe, yıl ile birlikte unique olur. SQL'deki tek DepartmentID unique kısıtlamasına uyuyoruz:
            modelBuilder.Entity<Budget>()
                .HasIndex(b => b.DepartmentID)
                .IsUnique();

            // SQL'de InvoiceNumber unique tanımlandı
            modelBuilder.Entity<Invoice>()
                .HasIndex(i => i.InvoiceNumber)
                .IsUnique();

            // --- 2. İlişki Tanımlamaları (FK'lar) ---

            // **1:1 İlişki: PurchaseRequest <-> PurchaseOrder (SQL'deki UNIQUE kısıtlaması nedeniyle)**
            // Bir PR, en fazla bir PO'ya sahip olabilir (PRID, PO tablosunda unique).
            modelBuilder.Entity<PurchaseOrder>()
                .HasOne(po => po.PurchaseRequest)
                .WithOne() // PR modelinde geri navigasyon tanımlanmadıysa WithOne() kullanılır
                .HasForeignKey<PurchaseOrder>(po => po.PRID)
                .IsRequired(); // NOT NULL kısıtlamasını da sağlar

            // **1:N İlişki: Supplier -> PurchaseOrders (SQL'de SupplierID FK)**
            modelBuilder.Entity<Supplier>()
                .HasMany(s => s.PurchaseOrders)
                .WithOne(po => po.Supplier)
                .HasForeignKey(po => po.SupplierID)
                .IsRequired();

            // **1:N İlişki: PurchaseOrder -> Invoices**
            modelBuilder.Entity<PurchaseOrder>()
                .HasMany(po => po.Invoices)
                .WithOne(i => i.PurchaseOrder)
                .HasForeignKey(i => i.POID)
                .IsRequired();

            // **1:N İlişki: PurchaseOrder -> GoodsReceipt**
            modelBuilder.Entity<PurchaseOrder>()
                .HasMany(po => po.GoodsReceipts)
                .WithOne(gr => gr.PurchaseOrder)
                .HasForeignKey(gr => gr.POID)
                .IsRequired();

            // **1:N İlişki: PurchaseOrder -> SupplierRatings**
            modelBuilder.Entity<PurchaseOrder>()
                .HasMany(po => po.SupplierRatings)
                .WithOne(sr => sr.PurchaseOrder)
                .HasForeignKey(sr => sr.POID)
                .IsRequired();

            // **Polimorfik İlişki (Attachments):** Sadece UploadedByUserID FK'sını tanımlıyoruz.
            modelBuilder.Entity<Attachment>()
                .HasOne(a => a.Uploader)
                .WithMany()
                .HasForeignKey(a => a.UploadedByUserID)
                .IsRequired();

            // **Çok Seviyeli Onay Akışı:** ApprovalHistory -> PurchaseRequest
            modelBuilder.Entity<ApprovalHistory>()
                .HasOne(ah => ah.PurchaseRequest)
                .WithMany(pr => pr.ApprovalHistories) // PR modelinde geri navigasyon varsayılıyor
                .HasForeignKey(ah => ah.PRID)
                .IsRequired();
        }
    }
}
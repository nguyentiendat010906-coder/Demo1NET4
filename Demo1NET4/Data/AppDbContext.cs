using Demo1NET4.Models;
using System.Data.Entity;

namespace Demo1NET4.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext() : base("name=DefaultConnection")
        {
            // Không tạo database mới - dùng database hiện có
            Database.SetInitializer<AppDbContext>(null);

#if DEBUG
            Database.Log = sql => System.Diagnostics.Debug.WriteLine(sql);
#endif
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceDetail> InvoiceDetails { get; set; }
        public DbSet<Table> Tables { get; set; }              
        public DbSet<TableGroup> TableGroups { get; set; }   
        public DbSet<Group> Groups { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Cấu hình User
            modelBuilder.Entity<User>()
                .ToTable("Users");
            modelBuilder.Entity<User>()
                .HasKey(u => u.Id);
            modelBuilder.Entity<User>()
                .Property(u => u.Username)
                .IsRequired()
                .HasMaxLength(100);
            modelBuilder.Entity<User>()
                .Property(u => u.PasswordHash)
                .IsRequired()
                .HasMaxLength(255);

            // Cấu hình decimal cho Invoice
            modelBuilder.Entity<Invoice>()
                .Property(i => i.Subtotal)
                .HasPrecision(18, 2);
            modelBuilder.Entity<Invoice>()
                .Property(i => i.VatAmount)
                .HasPrecision(18, 2);
            modelBuilder.Entity<Invoice>()
                .Property(i => i.TotalAmount)
                .HasPrecision(18, 2);

            // Cấu hình decimal cho Product
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            // Cấu hình relationship Table - TableGroup
            modelBuilder.Entity<Table>()
                .HasRequired(t => t.TableGroup)
                .WithMany(tg => tg.Tables)
                .HasForeignKey(t => t.TableGroupId)
                .WillCascadeOnDelete(false);

            base.OnModelCreating(modelBuilder);
        }
    }
}
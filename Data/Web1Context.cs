using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data
{
    public class Web1Context : DbContext
    {
        public Web1Context(DbContextOptions<Web1Context> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ItemClient>().HasKey(ic => new
            {
                ic.ItemId,
                ic.ClientId
            });
            modelBuilder.Entity<ItemClient>().HasOne(i => i.Item).WithMany(ic => ic.ItemClients).HasForeignKey(i => i.ItemId);
            modelBuilder.Entity<ItemClient>().HasOne(c => c.Client).WithMany(ic => ic.ItemClients).HasForeignKey(c => c.ClientId);

            modelBuilder.Entity<Items>().HasData(
                new Items { Id = 7, Name = "Hehe Product", Price = 200, SerialNumberId = 3 });

            modelBuilder.Entity<SerialNumber>().HasData(
                new SerialNumber { Id = 3, Name = "heh710", ItemId = 7 }
                );

            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Electronics" },
                new Category { Id = 2, Name = "Books" },
                new Category { Id = 3, Name = "Food" }
                );

            base.OnModelCreating(modelBuilder);
        }

        public DbSet<Items> Items { get; set; }
        public DbSet<SerialNumber> SerialNumbers { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<ItemClient> ItemClients { get; set; }
    }
}

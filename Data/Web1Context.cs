using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data
{
    public class Web1Context : DbContext // Changed to inherit from IdentityDbContext
    {
        public Web1Context(DbContextOptions<Web1Context> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Your existing configuration
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

            // Required: Call the base OnModelCreating for Identity configuration
            base.OnModelCreating(modelBuilder);
        }

        // Your existing DbSets
        public DbSet<Items> Items { get; set; }
        public DbSet<SerialNumber> SerialNumbers { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Client> Clients { get; set; }
        //public DbSet<ItemClient> ItemClients { get; set; }
    }
}
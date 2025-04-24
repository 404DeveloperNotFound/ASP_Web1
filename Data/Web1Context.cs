using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
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
            //modelBuilder.Entity<Items>().HasData(
            //    new Items { Id = 7, Name = "Hehe Product", Price = 200, SerialNumberId = 3 });

  
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Electronics" },
                new Category { Id = 2, Name = "Books" },
                new Category { Id = 3, Name = "Food" },
                new Category { Id = 4, Name = "Grocery" },
                new Category { Id = 5, Name = "Kitchen Utensils" },
                new Category { Id = 6, Name = "Snacks" },
                new Category { Id = 7, Name = "Decoration" },
                new Category { Id = 8, Name = "Gadgets" },
                new Category { Id = 9, Name = "Mobiles" },
                new Category { Id = 10, Name = "Laptops" },
                new Category { Id = 11, Name = "Home Appliances" },
                new Category { Id = 12, Name = "Monitor" },
                new Category { Id = 13, Name = "TV" },
                new Category { Id = 14, Name = "Keyboard" },
                new Category { Id = 15, Name = "AC" },
                new Category { Id = 16, Name = "Furniture" },
                new Category { Id = 17, Name = "Lights" },
                new Category { Id = 18, Name = "Vehicle parts" },
                new Category { Id = 19, Name = "Grooming" },
                new Category { Id = 20, Name = "Beauty" },
                new Category { Id = 21, Name = "Clothing" }
            );
            modelBuilder.Entity<Cart>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId);

            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Cart)
                .WithMany(c => c.Items)
                .HasForeignKey(ci => ci.CartId);

            modelBuilder.Entity<Items>()
                .HasIndex(i => i.SerialNumber)
                .IsUnique();

            modelBuilder.Entity<Client>().HasIndex(c => c.Email).IsUnique();

            modelBuilder.Entity<Client>().HasIndex(c => c.Username).IsUnique();

            base.OnModelCreating(modelBuilder);
        }

        public DbSet<Items> Items { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        //public DbSet<ItemClient> ItemClients { get; set; }
    }
}
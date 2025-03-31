using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data
{
    public class Web1Context : DbContext
    {
        public Web1Context(DbContextOptions<Web1Context> options) : base(options)
        {
        }

        public DbSet<Items> Items { get; set; }
    }
}

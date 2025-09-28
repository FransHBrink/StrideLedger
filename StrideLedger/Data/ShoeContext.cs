using Microsoft.EntityFrameworkCore;
using StrideLedger.Models;

namespace StrideLedger.Data
{
    public class ShoeContext : DbContext
    {
        public ShoeContext(DbContextOptions<ShoeContext> options) : base(options) { }

        public DbSet<Shoe> Shoes => Set<Shoe>();
        public DbSet<Run> Runs => Set<Run>();
    }
}


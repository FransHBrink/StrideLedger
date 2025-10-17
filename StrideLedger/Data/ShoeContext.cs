using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StrideLedger.Models;

namespace StrideLedger.Data
{
    public class ShoeContext : IdentityDbContext
    {
        public ShoeContext(DbContextOptions<ShoeContext> options) : base(options) { }

        public DbSet<Shoe> Shoes { get; set; }
        public DbSet<Run> Runs { get; set; }

        // New table for refresh tokens
        public DbSet<RefreshToken> RefreshTokens { get; set; }
    }
}


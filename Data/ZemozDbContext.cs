using Microsoft.EntityFrameworkCore;
using ZemozSmart.Models;

namespace ZemozSmart.Data
{
    public class ZemozDbContext : DbContext
    {
        public ZemozDbContext(DbContextOptions<ZemozDbContext> options) : base(options) { }

        public DbSet<Supporter> Supporters => Set<Supporter>();
        public DbSet<Card> Cards => Set<Card>();
        public DbSet<Scan> Scans => Set<Scan>();
        public DbSet<Agent> Agents => Set<Agent>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Card>()
                .HasOne(c => c.Supporter)
                .WithMany(s => s.Cards)
                .HasForeignKey(c => c.SupporterId);

            modelBuilder.Entity<Scan>()
                .HasOne(s => s.Card)
                .WithMany(c => c.Scans)
                .HasForeignKey(s => s.CardId);
        }
    }
}

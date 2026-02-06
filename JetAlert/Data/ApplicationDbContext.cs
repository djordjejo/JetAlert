using JetAlert.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JetAlert.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Flight> Flights { get; set; }
        public DbSet<PriceHistory> PriceHistory{ get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Flight>()
                .HasMany(f => f.PriceHistory)
                .WithOne(ph => ph.Flight)
                .HasForeignKey(ph => ph.FlightId);

            modelBuilder.Entity<Flight>().Property(f => f.Price).HasPrecision(18, 2);

            modelBuilder.Entity<PriceHistory>()
            .Property(p => p.Price)
            .HasPrecision(18, 2);

            base.OnModelCreating(modelBuilder);
        }
        
    }
}

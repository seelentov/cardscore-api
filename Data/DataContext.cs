using Microsoft.EntityFrameworkCore;
using cardscore_api.Models;
using System.Data;
using System.Reflection.Metadata;
using Microsoft.Extensions.Hosting;

namespace cardscore_api.Data
{
    public class DataContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<LeagueParseData> LeagueParseDatas { get; set; }
        public DbSet<League> Leagues { get; set; }
        public DbSet<Reglament> Reglaments { get; set; }
        public DbSet<Info> Infos { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<UserNotificationOption> UserNotificationOptions { get; set; }
        public DbSet<CachedNotification> CachedNotifications { get; set; }
        public string DbPath { get; }

        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<League>()
                .HasOne(l => l.Reglament)
                .WithOne(r => r.League)
                .HasForeignKey<League>(r => r.ReglamentId);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (optionsBuilder.IsConfigured)
            {
                return;
            }

            optionsBuilder
                .LogTo(Console.WriteLine, LogLevel.Debug);
        }
    }
}

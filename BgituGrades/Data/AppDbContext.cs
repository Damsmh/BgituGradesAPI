using BgituGrades.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BgituGrades.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<ApiKey> ApiKeys { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<Discipline> Disciplines { get; set; }
        public DbSet<Class> Classes { get; set; }
        public DbSet<Mark> Marks { get; set; }
        public DbSet<Presence> Presences { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Work> Works { get; set; }
        public DbSet<Transfer> Transfers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Class>()
                .Property(u => u.Type)
                .HasConversion(new EnumToStringConverter<ClassType>());
            modelBuilder.Entity<Presence>()
                .Property(u => u.IsPresent)
                .HasConversion(new EnumToStringConverter<PresenceType>());
            modelBuilder.Entity<ApiKey>().HasKey(k => k.Key);
        }
    }
}

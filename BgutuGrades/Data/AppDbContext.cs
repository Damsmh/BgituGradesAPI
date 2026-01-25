using BgutuGrades.Entities;
using Grades.Entities;
using Microsoft.EntityFrameworkCore;

namespace Grades.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Group> Groups { get; set; }
        public DbSet<Discipline> Disciplines { get; set; }
        public DbSet<Class> Classes { get; set; }
        public DbSet<Mark> Marks { get; set; }
        public DbSet<Presence> Presences { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Work> Works { get; set; }
        public DbSet<Transfer> Transfers { get; set; }
    }
}

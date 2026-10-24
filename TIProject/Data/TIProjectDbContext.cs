using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace TIProject.Data
{
    public class TIProjectDbContext : DbContext
    {
        public TIProjectDbContext(DbContextOptions<TIProjectDbContext> options) : base(options)
        {
        }
        public DbSet<User> Users { get; set; }
        public DbSet<Course> Courses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Borrar a un usuario no debe borrar en cascada los cursos que tiene a su cargo;
            // además, SQL Server no admite dos rutas de cascada hacia la tabla de inscripciones.
            modelBuilder.Entity<Course>()
                .HasOne(c => c.User)
                .WithMany(u => u.ManagedCourses)
                .HasForeignKey(c => c.IdManager)
                .OnDelete(DeleteBehavior.Restrict);
        }
    
    }
}

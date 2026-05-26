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
    
    }
}

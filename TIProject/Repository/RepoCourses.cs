using Microsoft.EntityFrameworkCore;
using TIProject.Data;

namespace TIProject.Repository
{
    public class RepoCourses : IRepoCourses
    {
        private readonly TIProjectDbContext _context;

        public RepoCourses(TIProjectDbContext context)
        {
            _context = context;
        }

        public async Task<List<Course>> GetAll()
        {
            return await _context.Courses.Include(b => b.User).AsNoTracking().ToListAsync();
        }

        public async Task<Course?> Get(int id)
        {
            return await _context.Courses.Include(b => b.User).FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<Course> Add(Course course)
        {
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();
            return course;
        }
        public async Task Update(Course course)
        {
            var existing = await _context.Courses.FindAsync(course.Id);
            if (existing == null)
                throw new InvalidOperationException("La brigada no existe.");

            _context.Entry(existing).CurrentValues.SetValues(course);
            await _context.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course != null)
            {
                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();
            }
        }
    }
}
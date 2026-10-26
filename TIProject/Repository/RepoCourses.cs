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
                throw new InvalidOperationException("El curso no existe.");

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

        // Cantidad de inscritos por curso, sin cargar a los usuarios (ni sus hashes) en memoria.
        public async Task<Dictionary<int, int>> GetEnrollmentCounts()
        {
            return await _context.Courses
                .Select(c => new { c.Id, Count = c.Users!.Count })
                .ToDictionaryAsync(x => x.Id, x => x.Count);
        }

        public async Task<List<Course>> GetEnrolled(int userId)
        {
            return await _context.Courses
                .Include(c => c.User)
                .Where(c => c.Users!.Any(u => u.Id == userId))
                .AsNoTracking()
                .ToListAsync();
        }

        // Devuelve false si el usuario ya estaba inscrito.
        public async Task<bool> Enroll(int courseId, int userId)
        {
            var course = await _context.Courses.Include(c => c.Users).FirstOrDefaultAsync(c => c.Id == courseId)
                ?? throw new InvalidOperationException("El curso no existe.");
            if (course.Date.Date < DateTime.Today)
                throw new InvalidOperationException("El curso ya se realizó.");
            if (course.Users!.Any(u => u.Id == userId))
                return false;

            var user = await _context.Users.FindAsync(userId)
                ?? throw new InvalidOperationException("El usuario no existe.");
            course.Users!.Add(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task Unenroll(int courseId, int userId)
        {
            var course = await _context.Courses.Include(c => c.Users).FirstOrDefaultAsync(c => c.Id == courseId);
            var user = course?.Users!.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                course!.Users!.Remove(user);
                await _context.SaveChangesAsync();
            }
        }
    }
}

using TIProject.Data;
using Microsoft.EntityFrameworkCore;

namespace TIProject.Repository
{
    public class RepoUsers : IRepoUsers
    {
        private readonly TIProjectDbContext _context;
        public RepoUsers(TIProjectDbContext context)
        {
            _context = context;
        }
        public async Task<User> Add(User user)
        {
            if (await _context.Users.AnyAsync(u => u.Email == user.Email))
                throw new InvalidOperationException("El correo ya está registrado.");

            if (await _context.Users.AnyAsync(u => u.IdNumber == user.IdNumber))
                throw new InvalidOperationException("El número de identificación ya está registrado.");

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<User?> Get(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<List<User>> GetAll()
        {
            return await _context.Users.AsNoTracking().ToListAsync();
        }

        public async Task Update(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
    }
}

using TIProject.Data;

namespace TIProject.Repository
{
    public interface IRepoUsers
    {
        Task<List<User>> GetAll();
        Task<User?> Get(int id);
        Task<User> Add(User user);
        Task Update(User user);
        Task Delete(int id);
    }
}

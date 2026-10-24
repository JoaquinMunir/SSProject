using TIProject.Data;

namespace TIProject.Repository
{
    public interface IRepoCourses
    {
        Task<List<Course>> GetAll();
        Task<Course?> Get(int id);
        Task<Course> Add(Course brigade);
        Task Update(Course brigade);
        Task Delete(int id);
        Task<List<Course>> GetEnrolled(int userId);
        Task<bool> Enroll(int courseId, int userId);
        Task Unenroll(int courseId, int userId);
    }
}

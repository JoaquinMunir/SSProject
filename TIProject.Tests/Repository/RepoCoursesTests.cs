using TIProject.Tests.Helpers;

namespace TIProject.Tests.Repository
{
    public class RepoCoursesTests
    {
        private static Course BuildCourse(string name = "Primeros Auxilios", int idManager = 1)
        {
            return new Course
            {
                Name = name,
                Date = new DateTime(2026, 1, 1),
                Place = "Aula 1",
                IdManager = idManager
            };
        }

        [Fact]
        public async Task Add_PersistsCourse()
        {
            using var context = DbContextFactory.CreateInMemory();
            var repo = new RepoCourses(context);

            var result = await repo.Add(BuildCourse());

            Assert.NotNull(result);
            Assert.True(result.Id > 0);
            Assert.Single(context.Courses);
        }

        [Fact]
        public async Task Get_ReturnsCourseWithUser()
        {
            using var context = DbContextFactory.CreateInMemory();

            var user = DbContextFactory.BuildValidUser();
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var repo = new RepoCourses(context);
            var added = await repo.Add(BuildCourse(idManager: user.Id));

            var fetched = await repo.Get(added.Id);

            Assert.NotNull(fetched);
            Assert.Equal(added.Id, fetched!.Id);
            Assert.NotNull(fetched.User);
            Assert.Equal(user.Email, fetched.User!.Email);
        }

        [Fact]
        public async Task Get_ReturnsNull_WhenMissing()
        {
            using var context = DbContextFactory.CreateInMemory();
            var repo = new RepoCourses(context);

            Assert.Null(await repo.Get(999));
        }

        [Fact]
        public async Task GetAll_ReturnsEveryCourse()
        {
            using var context = DbContextFactory.CreateInMemory();
            var manager = DbContextFactory.BuildValidUser();
            context.Users.Add(manager);
            await context.SaveChangesAsync();

            var repo = new RepoCourses(context);

            await repo.Add(BuildCourse(name: "A", idManager: manager.Id));
            await repo.Add(BuildCourse(name: "B", idManager: manager.Id));

            var all = await repo.GetAll();

            Assert.Equal(2, all.Count);
        }

        [Fact]
        public async Task Update_PersistsChanges()
        {
            using var context = DbContextFactory.CreateInMemory();
            var manager = DbContextFactory.BuildValidUser();
            context.Users.Add(manager);
            await context.SaveChangesAsync();

            var repo = new RepoCourses(context);
            var course = await repo.Add(BuildCourse(name: "Original", idManager: manager.Id));

            // Detach to mimic update from disconnected scenario
            context.Entry(course).State = EntityState.Detached;

            var updated = new Course
            {
                Id = course.Id,
                Name = "Actualizado",
                Date = course.Date,
                Place = course.Place,
                IdManager = course.IdManager
            };

            await repo.Update(updated);

            var fetched = await repo.Get(course.Id);
            Assert.NotNull(fetched);
            Assert.Equal("Actualizado", fetched!.Name);
        }

        [Fact]
        public async Task Update_Throws_WhenCourseMissing()
        {
            using var context = DbContextFactory.CreateInMemory();
            var repo = new RepoCourses(context);

            var ghost = BuildCourse();
            ghost.Id = 12345;

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => repo.Update(ghost));
            Assert.Equal("La brigada no existe.", ex.Message);
        }

        [Fact]
        public async Task Delete_RemovesCourse()
        {
            using var context = DbContextFactory.CreateInMemory();
            var repo = new RepoCourses(context);
            var course = await repo.Add(BuildCourse());

            await repo.Delete(course.Id);

            Assert.Null(await repo.Get(course.Id));
            Assert.Empty(context.Courses);
        }

        [Fact]
        public async Task Delete_IsNoOp_WhenIdNotFound()
        {
            using var context = DbContextFactory.CreateInMemory();
            var repo = new RepoCourses(context);
            await repo.Add(BuildCourse());

            await repo.Delete(999);

            Assert.Single(context.Courses);
        }
    }
}

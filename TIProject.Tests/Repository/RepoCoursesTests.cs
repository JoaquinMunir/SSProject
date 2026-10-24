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
            Assert.Equal("El curso no existe.", ex.Message);
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
    
        private static async Task<(TIProjectDbContext ctx, RepoCourses repo, User user, Course course)> SeedEnrollment(int daysFromToday = 7)
        {
            var context = DbContextFactory.CreateInMemory();
            var user = DbContextFactory.BuildValidUser(role: Role.student);
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var course = BuildCourse(idManager: user.Id);
            course.Date = DateTime.Today.AddDays(daysFromToday);
            context.Courses.Add(course);
            await context.SaveChangesAsync();

            return (context, new RepoCourses(context), user, course);
        }

        [Fact]
        public async Task Enroll_AddsUserToCourse()
        {
            var (context, repo, user, course) = await SeedEnrollment();
            using var _ = context;

            var added = await repo.Enroll(course.Id, user.Id);

            Assert.True(added);
            var enrolled = await repo.GetEnrolled(user.Id);
            Assert.Single(enrolled);
            Assert.Equal(course.Id, enrolled[0].Id);
        }

        [Fact]
        public async Task Enroll_Twice_ReturnsFalse()
        {
            var (context, repo, user, course) = await SeedEnrollment();
            using var _ = context;

            await repo.Enroll(course.Id, user.Id);
            var addedAgain = await repo.Enroll(course.Id, user.Id);

            Assert.False(addedAgain);
            Assert.Single(await repo.GetEnrolled(user.Id));
        }

        [Fact]
        public async Task Enroll_PastCourse_Throws()
        {
            var (context, repo, user, course) = await SeedEnrollment(daysFromToday: -1);
            using var _ = context;

            await Assert.ThrowsAsync<InvalidOperationException>(() => repo.Enroll(course.Id, user.Id));
        }

        [Fact]
        public async Task Unenroll_RemovesUserFromCourse()
        {
            var (context, repo, user, course) = await SeedEnrollment();
            using var _ = context;

            await repo.Enroll(course.Id, user.Id);
            await repo.Unenroll(course.Id, user.Id);

            Assert.Empty(await repo.GetEnrolled(user.Id));
        }
    }
}

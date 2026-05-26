using TIProject.Tests.Helpers;

namespace TIProject.Tests.Repository
{
    public class RepoUsersTests
    {
        [Fact]
        public async Task Add_PersistsUser_AndReturnsIt()
        {
            using var context = DbContextFactory.CreateInMemory();
            var repo = new RepoUsers(context);
            var user = DbContextFactory.BuildValidUser();

            var result = await repo.Add(user);

            Assert.NotNull(result);
            Assert.True(result.Id > 0);
            Assert.Single(context.Users);
        }

        [Fact]
        public async Task Add_ThrowsWhenEmailAlreadyExists()
        {
            using var context = DbContextFactory.CreateInMemory();
            var repo = new RepoUsers(context);

            await repo.Add(DbContextFactory.BuildValidUser(idNumber: 11111111, email: "dup@example.com"));

            var duplicate = DbContextFactory.BuildValidUser(idNumber: 22222222, email: "dup@example.com");

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => repo.Add(duplicate));
            Assert.Equal("El correo ya está registrado.", ex.Message);
        }

        [Fact]
        public async Task Add_ThrowsWhenIdNumberAlreadyExists()
        {
            using var context = DbContextFactory.CreateInMemory();
            var repo = new RepoUsers(context);

            await repo.Add(DbContextFactory.BuildValidUser(idNumber: 33333333, email: "a@example.com"));

            var duplicate = DbContextFactory.BuildValidUser(idNumber: 33333333, email: "b@example.com");

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => repo.Add(duplicate));
            Assert.Equal("El número de identificación ya está registrado.", ex.Message);
        }

        [Fact]
        public async Task Get_ReturnsUser_WhenExists()
        {
            using var context = DbContextFactory.CreateInMemory();
            var repo = new RepoUsers(context);
            var user = await repo.Add(DbContextFactory.BuildValidUser());

            var fetched = await repo.Get(user.Id);

            Assert.NotNull(fetched);
            Assert.Equal(user.Email, fetched!.Email);
        }

        [Fact]
        public async Task Get_ReturnsNull_WhenMissing()
        {
            using var context = DbContextFactory.CreateInMemory();
            var repo = new RepoUsers(context);

            var fetched = await repo.Get(999);

            Assert.Null(fetched);
        }

        [Fact]
        public async Task GetAll_ReturnsEveryUser()
        {
            using var context = DbContextFactory.CreateInMemory();
            var repo = new RepoUsers(context);

            await repo.Add(DbContextFactory.BuildValidUser(idNumber: 44444444, email: "u1@example.com"));
            await repo.Add(DbContextFactory.BuildValidUser(idNumber: 55555555, email: "u2@example.com"));

            var all = await repo.GetAll();

            Assert.Equal(2, all.Count);
        }

        [Fact]
        public async Task Update_PersistsChanges()
        {
            using var context = DbContextFactory.CreateInMemory();
            var repo = new RepoUsers(context);
            var user = await repo.Add(DbContextFactory.BuildValidUser());

            user.Name = "Nombre Actualizado";
            await repo.Update(user);

            var fetched = await repo.Get(user.Id);
            Assert.Equal("Nombre Actualizado", fetched!.Name);
        }

        [Fact]
        public async Task Delete_RemovesUser()
        {
            using var context = DbContextFactory.CreateInMemory();
            var repo = new RepoUsers(context);
            var user = await repo.Add(DbContextFactory.BuildValidUser());

            await repo.Delete(user.Id);

            Assert.Null(await repo.Get(user.Id));
            Assert.Empty(context.Users);
        }

        [Fact]
        public async Task Delete_IsNoOp_WhenIdNotFound()
        {
            using var context = DbContextFactory.CreateInMemory();
            var repo = new RepoUsers(context);
            await repo.Add(DbContextFactory.BuildValidUser());

            await repo.Delete(999);

            Assert.Single(context.Users);
        }
    }
}

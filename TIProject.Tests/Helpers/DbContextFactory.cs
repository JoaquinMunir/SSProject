namespace TIProject.Tests.Helpers
{
    internal static class DbContextFactory
    {
        public static TIProjectDbContext CreateInMemory()
        {
            var options = new DbContextOptionsBuilder<TIProjectDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new TIProjectDbContext(options);
        }

        public static User BuildValidUser(
            int? idNumber = 12345678,
            string email = "test@example.com",
            Role role = Role.admin)
        {
            return new User
            {
                Name = "Juan Pérez",
                IdNumber = idNumber,
                Email = email,
                Password = "Secret123",
                PhoneNumber = "1234567890",
                Address = "Calle Falsa 123",
                School = "ESC",
                Blood = "O+",
                Role = role
            };
        }
    }
}

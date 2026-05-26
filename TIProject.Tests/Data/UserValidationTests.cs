using System.ComponentModel.DataAnnotations;
using TIProject.Tests.Helpers;

namespace TIProject.Tests.Data
{
    public class UserValidationTests
    {
        private static (bool IsValid, List<ValidationResult> Results) Validate(User user)
        {
            var ctx = new ValidationContext(user);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(user, ctx, results, validateAllProperties: true);
            return (isValid, results);
        }

        [Fact]
        public void ValidUser_PassesValidation()
        {
            var user = DbContextFactory.BuildValidUser();

            var (isValid, results) = Validate(user);

            Assert.True(isValid, string.Join("; ", results.Select(r => r.ErrorMessage)));
        }

        [Fact]
        public void Brigader_WithoutBrigade_FailsIValidatable()
        {
            var user = DbContextFactory.BuildValidUser(role: Role.brigader);
            user.Brigade = null;

            var errors = user.Validate(new ValidationContext(user)).ToList();

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(User.Brigade)));
        }

        [Fact]
        public void Brigader_WithBrigade_PassesIValidatable()
        {
            var user = DbContextFactory.BuildValidUser(role: Role.brigader);
            user.Brigade = Brigade.firstAid;

            var errors = user.Validate(new ValidationContext(user)).ToList();

            Assert.Empty(errors);
        }

        [Theory]
        [InlineData("Juan123")]
        [InlineData("María_")]
        [InlineData("Pedro@")]
        public void Name_WithInvalidCharacters_FailsValidation(string name)
        {
            var user = DbContextFactory.BuildValidUser();
            user.Name = name;

            var (isValid, results) = Validate(user);

            Assert.False(isValid);
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(User.Name)));
        }

        [Theory]
        [InlineData(123)]          // too short
        [InlineData(123456789)]    // too long
        public void IdNumber_OutOfRange_FailsValidation(int idNumber)
        {
            var user = DbContextFactory.BuildValidUser(idNumber: idNumber);

            var (isValid, results) = Validate(user);

            Assert.False(isValid);
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(User.IdNumber)));
        }

        [Theory]
        [InlineData("invalid-email")]
        [InlineData("foo@")]
        [InlineData("foo")]
        public void Email_Invalid_FailsValidation(string email)
        {
            var user = DbContextFactory.BuildValidUser(email: email);

            var (isValid, results) = Validate(user);

            Assert.False(isValid);
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(User.Email)));
        }

        [Theory]
        [InlineData("short")]            // < 8 chars
        [InlineData("has_spaces_here")]  // contains spaces
        [InlineData("waytoolongpasswordvalue123")] // > 20 chars
        public void Password_Invalid_FailsValidation(string password)
        {
            var user = DbContextFactory.BuildValidUser();
            user.Password = password;

            var (isValid, results) = Validate(user);

            Assert.False(isValid);
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(User.Password)));
        }

        [Theory]
        [InlineData("123")]         // too short
        [InlineData("12345678901")] // too long
        [InlineData("abcdefghij")]  // non-numeric
        public void PhoneNumber_Invalid_FailsValidation(string phone)
        {
            var user = DbContextFactory.BuildValidUser();
            user.PhoneNumber = phone;

            var (isValid, results) = Validate(user);

            Assert.False(isValid);
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(User.PhoneNumber)));
        }

        [Fact]
        public void MissingRequiredFields_FailsValidation()
        {
            var user = new User();

            var (isValid, results) = Validate(user);

            Assert.False(isValid);
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(User.Name)));
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(User.Email)));
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(User.Password)));
        }
    }
}

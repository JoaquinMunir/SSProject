using System.ComponentModel.DataAnnotations;
using TIProject.Tests.Helpers;

namespace TIProject.Tests.Data
{
    public class CourseValidationTests
    {
        private static Course BuildValidCourse(User? manager = null) => new()
        {
            Name = "Curso",
            Date = new DateTime(2026, 1, 1),
            Place = "Aula 1",
            IdManager = manager?.Id ?? 1,
            User = manager
        };

        [Fact]
        public void ValidCourse_PassesValidation()
        {
            var course = BuildValidCourse();
            var ctx = new ValidationContext(course);
            var results = new List<ValidationResult>();

            var isValid = Validator.TryValidateObject(course, ctx, results, validateAllProperties: true);

            Assert.True(isValid, string.Join("; ", results.Select(r => r.ErrorMessage)));
        }

        [Fact]
        public void Course_MissingRequiredFields_FailsValidation()
        {
            var course = new Course();
            var ctx = new ValidationContext(course);
            var results = new List<ValidationResult>();

            var isValid = Validator.TryValidateObject(course, ctx, results, validateAllProperties: true);

            Assert.False(isValid);
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(Course.Name)));
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(Course.Place)));
        }

        [Fact]
        public void Course_ManagerNotAdmin_FailsIValidatable()
        {
            var manager = DbContextFactory.BuildValidUser(role: Role.brigader);
            var course = BuildValidCourse(manager);

            var errors = course.Validate(new ValidationContext(course)).ToList();

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Course.IdManager)));
            Assert.Equal("El encargado debe ser administrador.", errors[0].ErrorMessage);
        }

        [Fact]
        public void Course_ManagerIsAdmin_PassesIValidatable()
        {
            var manager = DbContextFactory.BuildValidUser(role: Role.admin);
            var course = BuildValidCourse(manager);

            var errors = course.Validate(new ValidationContext(course)).ToList();

            Assert.Empty(errors);
        }

        [Fact]
        public void Course_NullManager_PassesIValidatable()
        {
            // When User isn't loaded the rule is skipped (User == null)
            var course = BuildValidCourse(manager: null);

            var errors = course.Validate(new ValidationContext(course)).ToList();

            Assert.Empty(errors);
        }
    }
}

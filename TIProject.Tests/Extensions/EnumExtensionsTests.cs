using System.ComponentModel.DataAnnotations;
using TIProject.Extensions;

namespace TIProject.Tests.Extensions
{
    public class EnumExtensionsTests
    {
        public enum NoDisplay
        {
            Plain,
        }

        public enum WithDisplay
        {
            [Display(Name = "Bonito")]
            Pretty,
        }

        [Theory]
        [InlineData(Brigade.firstAid, "Primeros Auxilios")]
        [InlineData(Brigade.evacuation, "Evacuación")]
        [InlineData(Brigade.fires, "Combate de Incendios")]
        [InlineData(Brigade.rescue, "Busqueda y Resate")]
        [InlineData(Brigade.communication, "Comunicación")]
        public void GetDisplayName_ReturnsDisplayName_ForBrigade(Brigade value, string expected)
        {
            Assert.Equal(expected, value.GetDisplayName());
        }

        [Theory]
        [InlineData(Role.admin, "Administrador")]
        [InlineData(Role.brigaderChief, "Jefe de Brigada")]
        [InlineData(Role.brigader, "Brigadista")]
        [InlineData(Role.auxiliar, "Auxiliar")]
        [InlineData(Role.student, "Estudiante")]
        public void GetDisplayName_ReturnsDisplayName_ForRole(Role value, string expected)
        {
            Assert.Equal(expected, value.GetDisplayName());
        }

        [Fact]
        public void GetDisplayName_FallsBackToToString_WhenNoDisplayAttribute()
        {
            Assert.Equal("Plain", NoDisplay.Plain.GetDisplayName());
        }

        [Fact]
        public void GetDisplayName_ReturnsAttributeName_WhenPresent()
        {
            Assert.Equal("Bonito", WithDisplay.Pretty.GetDisplayName());
        }
    }
}

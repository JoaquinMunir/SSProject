using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Timers;
using Microsoft.EntityFrameworkCore;

namespace TIProject.Data
{
    public enum Brigade
    {
        [Display(Name = "Primeros Auxilios")]
        firstAid,

        [Display(Name = "Evacuación")]
        evacuation,

        [Display(Name = "Combate de Incendios")]
        fires,

        [Display(Name = "Busqueda y Resate")]
        rescue,

        [Display(Name = "Comunicación")]
        communication,
    }

    public enum Role
    {
        [Display(Name = "Administrador")]
        admin,

        [Display(Name = "Jefe de Brigada")]
        brigaderChief,

        [Display(Name = "Brigadista")]
        brigader,

        [Display(Name = "Auxiliar")]
        auxiliar,

        [Display(Name = "Estudiante")]
        student,
    }
    public class User : IValidatableObject
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El campo 'Nombre' es obligatorio.")]
        [RegularExpression(@"^[a-zA-ZÁÉÍÓÚáéíóúÑñ\s]+$", ErrorMessage = "El nombre solo puede contener letras y espacios.")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "El campo 'Número de Identificación' es obligatorio.")]
        [Range(10000000, 99999999, ErrorMessage = "El número de identificación debe contener 8 dígitos.")]
        public int? IdNumber { get; set; }

        [Required(ErrorMessage = "El campo 'Correo electrónico' es obligatorio.")]
        [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "El campo 'Contraseña' es obligatorio.")]
        [MaxLength(512)] // Espacio para hash PBKDF2 (~84 chars) + margen. Validación de longitud de texto plano se hace en la capa de aplicación.
        public string? Password { get; set; }

        [Required(ErrorMessage = "El campo 'Teléfono' es obligatorio.")]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "El número de telefono debe tener exactamente 10 dígitos.")]
        [RegularExpression("^[0-9]*$", ErrorMessage = "El campo solo debe contener números.")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "El campo 'Domicilio' es obligatorio.")]
        public string? Address { get; set; }

        [Required(ErrorMessage = "El campo 'Escuela' es obligatorio.")]
        public string? School { get; set; }

        public Brigade? Brigade { get; set; }

        [Required(ErrorMessage = "El campo 'Tipo de Sangre' es obligatorio.")]
        public string? Blood { get; set; }

        [Required(ErrorMessage = "El campo 'Rol' es obligatorio.")]
        public Role? Role { get; set; }

        [InverseProperty("User")]
        public virtual ICollection<Course>? ManagedCourses { get; set; }
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Role == Data.Role.brigader && Brigade == null)
            {
                yield return new ValidationResult(
                    "El campo 'Tipo de Brigada' es obligatorio para los brigadistas.",
                    new[] { nameof(Brigade) }
                );
            }
        }
    }
}

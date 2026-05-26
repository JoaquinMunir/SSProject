using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Timers;

namespace TIProject.Data
{
    public class Course : IValidatableObject
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El campo 'Nombre' es obligatorio.")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "El campo 'Fecha' es obligatorio.")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "El campo 'Lugar' es obligatorio.")]
        public string? Place { get; set; }

        public int IdManager { get; set; }

        [ForeignKey("IdManager")]
        [InverseProperty("ManagedCourses")]
        public virtual User? User { get; set; }

        public virtual ICollection<User>? Users { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (User != null && User.Role != Role.admin)
            {
                yield return new ValidationResult(
                    "El encargado debe ser administrador.",
                    new[] { nameof(IdManager) });
            }
        }
    }
}
using System.ComponentModel.DataAnnotations;

namespace AdministracionSoluciones.Entities.Tareas;

// Campos editables de la tarea y datos de apoyo para mostrar el formulario.
public class TareaFormulario
{
    [Display(Name = "Descripción de la tarea")]
    public string? Descripcion { get; set; }

    [Display(Name = "Fecha"), DataType(DataType.Date)]
    public DateTime? Fecha { get; set; }

    [Display(Name = "Usuario"), Required(ErrorMessage = "Seleccione un usuario.")]
    public int? UsuarioId { get; set; }

    [Display(Name = "Horas invertidas")]
    public decimal? Horas { get; set; }

    // Estos datos se cargan en el servidor; no forman parte de los campos editables.
    public int? TareaId { get; set; }
    public SolicitudOpcion Solicitud { get; set; } = new();
    public IReadOnlyList<UsuarioOpcion> Usuarios { get; set; } = [];
}

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using AdministracionSoluciones.Entities;
using AdministracionSoluciones.Infraestructura;
namespace AdministracionSoluciones.Models.Tareas;
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
    [ModelBinder(BinderType = typeof(HorasModelBinder))]
    public decimal? Horas { get; set; }

    // Estos datos se cargan en el servidor; no forman parte de los campos editables.
    [ValidateNever]
    public int? TareaId { get; set; }
    [ValidateNever]
    public SolicitudOpcion Solicitud { get; set; } = new();
    [ValidateNever]
    public IReadOnlyList<UsuarioOpcion> Usuarios { get; set; } = [];
}

// Información del listado de tareas y su paginación.
public class ListaTareas
{
    public SolicitudOpcion? Solicitud { get; set; }
    public IReadOnlyList<TareaFila> Filas { get; set; } = [];
    public int Pagina { get; set; } = 1;
    public long Cantidad { get; set; }
    // Mantiene al menos una página aunque la solicitud no tenga tareas.
    public long Paginas => Math.Max(1, (Cantidad + 9) / 10);
}

// Filtros obligatorios para consultar un reporte mensual.
public class FiltroReporte
{
    [Required(ErrorMessage = "Seleccione un mes."), Range(1, 12, ErrorMessage = "Seleccione un mes válido.")]
    public int? Mes { get; set; }
    [Display(Name = "Año"), Required(ErrorMessage = "Indique el año."), Range(1000, 9999, ErrorMessage = "Indique un año entre 1000 y 9999.")]
    public int? Anio { get; set; }
    [Required(ErrorMessage = "Seleccione un elemento para consultar.")]
    public int? SeleccionId { get; set; }
    public bool Consultar { get; set; }
}

// Resultados y total de horas del reporte seleccionado.
public class ReporteTareas
{
    public FiltroReporte Filtro { get; set; } = new();
    public bool PorUsuario { get; set; }
    public IReadOnlyList<UsuarioOpcion> Usuarios { get; set; } = [];
    public IReadOnlyList<SolicitudOpcion> Solicitudes { get; set; } = [];
    public IReadOnlyList<TareaFila>? Filas { get; set; }
    // Las tareas sin horas registradas no aportan al total.
    public decimal TotalHoras
    {
        get
        {
            decimal total = 0;
            if (Filas != null)
            {
                foreach (var tarea in Filas)
                {
                    total += tarea.Horas ?? 0;
                }
            }
            return total;
        }
    }
    public string Seleccion { get; set; } = "";
}

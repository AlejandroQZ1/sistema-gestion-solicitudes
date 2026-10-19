using System.ComponentModel.DataAnnotations;

namespace AdministracionSoluciones.Entities.Tareas;

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

using AdministracionSoluciones.Models.Tareas;
using AdministracionSoluciones.Services.Abstract;
using Microsoft.AspNetCore.Mvc;
namespace AdministracionSoluciones.Pages.ReportesTareas;

// TAR3: reporte mensual con el filtro correspondiente.
public class PorSolicitudModel : ReporteModel
{
    public PorSolicitudModel(ITareaService tareas) : base(tareas)
    {
    }

    public Task<IActionResult> OnGetAsync(FiltroReporte filtro)
    {
        return ConsultarAsync(filtro, false);
    }
}

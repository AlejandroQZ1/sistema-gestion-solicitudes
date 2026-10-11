using AdministracionSoluciones.Models.Tareas;
using AdministracionSoluciones.Services.Abstract;
using Microsoft.AspNetCore.Mvc;
namespace AdministracionSoluciones.Pages.ReportesTareas;

// TAR2: reporte mensual con el filtro correspondiente.
public class PorUsuarioModel : ReporteModel
{
    public PorUsuarioModel(ITareaService tareas) : base(tareas)
    {
    }

    public Task<IActionResult> OnGetAsync(FiltroReporte filtro)
    {
        return ConsultarAsync(filtro, true);
    }
}

using AdministracionSoluciones.Infraestructura;
using AdministracionSoluciones.Models.Tareas;
using AdministracionSoluciones.Seguridad;
using AdministracionSoluciones.Services.Abstract;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AdministracionSoluciones.Pages.ReportesTareas;

// Valida los filtros comunes y carga los datos de los dos reportes mensuales.
[ServiceFilter(typeof(ErroresTareas))]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public abstract class ReporteModel : PageModel
{
    private readonly ITareaService tareas;
    public ReporteTareas Datos { get; set; } = new();

    protected ReporteModel(ITareaService tareas)
    {
        this.tareas = tareas;
    }

    protected async Task<IActionResult> ConsultarAsync(FiltroReporte filtro, bool porUsuario)
    {
        Datos.Filtro = filtro;
        Datos.PorUsuario = porUsuario;
        if (porUsuario)
        {
            Datos.Usuarios = await tareas.UsuariosAsync();
        }
        else
        {
            Datos.Solicitudes = await tareas.SolicitudesAsync();
        }
        if (!filtro.Consultar)
        {
            ModelState.Clear();
            await tareas.ConsultaAsync(Sesion.NombreUsuario(HttpContext), porUsuario ? "reporte por usuario" : "reporte por solicitud");
            return Page();
        }
        var existe = porUsuario ? Datos.Usuarios.Any(u => u.Id == filtro.SeleccionId) : Datos.Solicitudes.Any(s => s.Id == filtro.SeleccionId);
        if (filtro.SeleccionId.HasValue && !existe)
        {
            ModelState.AddModelError("SeleccionId", porUsuario ? "Seleccione un usuario existente." : "Seleccione una solicitud existente.");
        }
        if (ModelState.IsValid)
        {
            Datos.Filas = await tareas.ReporteAsync(porUsuario, filtro.SeleccionId!.Value, filtro.Mes!.Value, filtro.Anio!.Value, Sesion.NombreUsuario(HttpContext));
            Datos.Seleccion = porUsuario
                ? Datos.Usuarios.Single(u => u.Id == filtro.SeleccionId).Etiqueta
                : Datos.Solicitudes.Single(s => s.Id == filtro.SeleccionId).Etiqueta;
        }
        else
        {
            await tareas.ConsultaAsync(Sesion.NombreUsuario(HttpContext), porUsuario ? "reporte por usuario" : "reporte por solicitud");
        }
        return Page();
    }
}

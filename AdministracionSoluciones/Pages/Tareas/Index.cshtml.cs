using AdministracionSoluciones.Infraestructura;
using AdministracionSoluciones.Models.Tareas;
using AdministracionSoluciones.Seguridad;
using AdministracionSoluciones.Services.Abstract;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;

namespace AdministracionSoluciones.Pages.Tareas;

// TAR1: pagina principal del mantenimiento y confirmacion de eliminacion.
[ServiceFilter(typeof(ErroresTareas))]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public class IndexModel : PageModel
{
    private readonly ITareaService tareas;
    public ListaTareas Datos { get; set; } = new();

    public IndexModel(ITareaService tareas)
    {
        this.tareas = tareas;
    }

    // La solicitud identifica el contexto; las paginas contienen diez tareas.
    public async Task<IActionResult> OnGetAsync(int? solicitudId, int pagina = 1)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }
        // Desde el menú se listan todas las tareas; desde una solicitud, solo las asociadas.
        if (solicitudId.HasValue)
        {
            Datos.Solicitud = await tareas.SolicitudAsync(solicitudId.Value);
            if (Datos.Solicitud == null)
            {
                return NotFound();
            }
        }
        Datos.Pagina = Math.Max(1, pagina);
        var resultado = await tareas.ListarAsync(solicitudId, Datos.Pagina, Sesion.NombreUsuario(HttpContext));
        Datos.Filas = resultado.Filas;
        Datos.Cantidad = resultado.Cantidad;
        if (Datos.Pagina > Datos.Paginas)
        {
            return RedirectToPage("Index", new { solicitudId, pagina = Datos.Paginas });
        }
        return Page();
    }

    // La FK evita eliminar tareas relacionadas.
    public async Task<IActionResult> OnPostEliminarAsync(int id, int solicitudId, bool todas = false)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }
        try
        {
            if (!await tareas.EliminarAsync(id, solicitudId, Sesion.NombreUsuario(HttpContext)))
            {
                return NotFound();
            }
            TempData["ExitoTareas"] = "La tarea se eliminó correctamente.";
        }
        catch (MySqlException ex) when (ex.Number == 1451)
        {
            await tareas.RegistrarErrorAsync(Sesion.NombreUsuario(HttpContext), ex, HttpContext.TraceIdentifier);
            TempData["ErrorTareas"] = "No se puede eliminar un registro con datos relacionados.";
        }
        return todas ? RedirectToPage("Index") : RedirectToPage("Index", new { solicitudId });
    }
}

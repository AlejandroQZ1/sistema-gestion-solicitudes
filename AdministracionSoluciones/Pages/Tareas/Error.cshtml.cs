using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AdministracionSoluciones.Pages.Tareas;

// Muestra el error de tareas sin depender de los servicios que pudieron fallar.
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public class ErrorModel : PageModel
{
    public void OnGet()
    {
        Response.StatusCode = StatusCodes.Status500InternalServerError;
        TempData["ErrorTareas"] = "No se pudo completar la operación. Intente nuevamente.";
    }
}

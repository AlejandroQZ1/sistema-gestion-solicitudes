using AdministracionSoluciones.Seguridad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AdministracionSoluciones.Pages.Login
{
    /// <summary>Cierra la sesión y regresa al login.</summary>
    [PermitirSinSesion]
    public class CerrarSesionModel : PageModel
    {
        public IActionResult OnGet()
        {
            Sesion.Cerrar(HttpContext);
            return RedirectToPage("/Login/Index");
        }
    }
}

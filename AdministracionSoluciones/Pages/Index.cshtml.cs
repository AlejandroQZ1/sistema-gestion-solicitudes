using AdministracionSoluciones.Seguridad;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AdministracionSoluciones.Pages
{
    /// <summary>USR2 - Página de bienvenida después del login.</summary>
    public class IndexModel : PageModel
    {
        public string NombreCompleto { get; set; } = string.Empty;

        public void OnGet()
        {
            NombreCompleto = Sesion.NombreCompleto(HttpContext);
        }
    }
}

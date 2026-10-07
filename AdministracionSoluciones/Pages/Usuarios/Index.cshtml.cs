using AdministracionSoluciones.Entities;
using AdministracionSoluciones.Seguridad;
using AdministracionSoluciones.Services.Abstract;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AdministracionSoluciones.Pages.Usuarios
{
    /// <summary>USR4 - Listado de usuarios y cambio de estado desde el listado.</summary>
    public class IndexModel : PageModel
    {
        private readonly IUsuarioService _usuarioService;

        public IndexModel(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
            Usuarios = new List<Usuario>();
        }

        public IEnumerable<Usuario> Usuarios { get; set; }

        public int? UsuarioSesionID { get; set; }

        public async Task OnGetAsync()
        {
            Usuarios = await _usuarioService.GetAllAsync();
            UsuarioSesionID = Sesion.UsuarioID(HttpContext);
        }

        /// <summary>Botones Activar / Inactivar / Bloquear del listado.</summary>
        public async Task<IActionResult> OnPostCambiarEstadoAsync(int id, string estado)
        {
            var resultado = await _usuarioService.CambiarEstadoAsync(
                id, estado, Sesion.UsuarioID(HttpContext), Sesion.NombreUsuario(HttpContext));

            TempData[resultado.Exito ? "Exito" : "Error"] = resultado.Mensaje;
            return RedirectToPage("Index");
        }
    }
}

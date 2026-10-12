using AdministracionSoluciones.Entities;
using AdministracionSoluciones.Seguridad;
using AdministracionSoluciones.Services.Abstract;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AdministracionSoluciones.Pages.Usuarios
{
    /// <summary>USR4 - Eliminar usuario (solo si no tiene datos relacionados).</summary>
    public class DeleteModel : PageModel
    {
        private readonly IUsuarioService _usuarioService;

        public DeleteModel(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        public Usuario? Usuario { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Usuario = await _usuarioService.GetByIdAsync(id);

            if (Usuario == null)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var resultado = await _usuarioService.EliminarAsync(
                id, Sesion.UsuarioID(HttpContext), Sesion.NombreUsuario(HttpContext));

            TempData[resultado.Exito ? "Exito" : "Error"] = resultado.Mensaje;
            return RedirectToPage("Index");
        }
    }
}

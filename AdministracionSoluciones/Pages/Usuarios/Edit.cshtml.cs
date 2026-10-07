using AdministracionSoluciones.Seguridad;
using AdministracionSoluciones.Services.Abstract;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AdministracionSoluciones.Pages.Usuarios
{
    /// <summary>USR4 - Actualizar usuario.</summary>
    public class EditModel : PageModel
    {
        private readonly IUsuarioService _usuarioService;

        public EditModel(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
            Input = new UsuarioInput();
        }

        [BindProperty]
        public UsuarioInput Input { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var usuario = await _usuarioService.GetByIdAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }

            Input = UsuarioInput.FromUsuario(usuario);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            Input.UsuarioID = id;
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var idSesion = Sesion.UsuarioID(HttpContext);
            var resultado = await _usuarioService.ActualizarAsync(
                Input.ToUsuario(), Input.Contrasena, idSesion, Sesion.NombreUsuario(HttpContext));

            if (!resultado.Exito)
            {
                ModelState.AddModelError(resultado.Campo == null ? string.Empty : "Input." + resultado.Campo, resultado.Mensaje);
                return Page();
            }

            // Si el usuario se editó a sí mismo, se actualiza el nombre que aparece en el encabezado
            if (id == idSesion)
            {
                var usuario = await _usuarioService.GetByIdAsync(id);
                if (usuario != null)
                {
                    Sesion.Iniciar(HttpContext, usuario);
                }
            }

            TempData["Exito"] = resultado.Mensaje;
            return RedirectToPage("Index");
        }
    }
}

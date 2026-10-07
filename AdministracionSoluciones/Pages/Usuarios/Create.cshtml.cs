using AdministracionSoluciones.Seguridad;
using AdministracionSoluciones.Services.Abstract;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AdministracionSoluciones.Pages.Usuarios
{
    /// <summary>USR4 - Crear usuario.</summary>
    public class CreateModel : PageModel
    {
        private readonly IUsuarioService _usuarioService;

        public CreateModel(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
            Input = new UsuarioInput();
        }

        [BindProperty]
        public UsuarioInput Input { get; set; }

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Input.UsuarioID = 0;
            if (string.IsNullOrWhiteSpace(Input.Contrasena))
            {
                ModelState.AddModelError("Input.Contrasena", "La contraseña es requerida.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var resultado = await _usuarioService.CrearAsync(
                Input.ToUsuario(), Input.Contrasena, Sesion.NombreUsuario(HttpContext));

            if (!resultado.Exito)
            {
                ModelState.AddModelError(resultado.Campo == null ? string.Empty : "Input." + resultado.Campo, resultado.Mensaje);
                return Page();
            }

            TempData["Exito"] = resultado.Mensaje;
            return RedirectToPage("Index");
        }
    }
}

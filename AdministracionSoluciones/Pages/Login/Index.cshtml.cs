using AdministracionSoluciones.Seguridad;
using AdministracionSoluciones.Services.Abstract;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AdministracionSoluciones.Pages.Login
{
    /// <summary>USR1 - Pantalla de login.</summary>
    [PermitirSinSesion]
    public class IndexModel : PageModel
    {
        public const string MensajeCredencialesIncorrectas = "Usuario y/o contraseña incorrectos.";

        private readonly ILoginService _loginService;

        public IndexModel(ILoginService loginService)
        {
            _loginService = loginService;
            Input = new LoginInput();
        }

        [BindProperty]
        public LoginInput Input { get; set; }

        /// <summary>Aviso, por ejemplo cuando intentó entrar a una página sin sesión.</summary>
        public string? Mensaje { get; set; }

        public string? Error { get; set; }

        public IActionResult OnGet()
        {
            if (Sesion.EstaIniciada(HttpContext))
            {
                return RedirectToPage("/Index");
            }

            Mensaje = TempData["MensajeLogin"] as string;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var usuario = await _loginService.IniciarSesionAsync(Input.NombreUsuario, Input.Contrasena);

            if (usuario == null)
            {
                Error = MensajeCredencialesIncorrectas;
                Input.Contrasena = null;
                return Page();
            }

            Sesion.Iniciar(HttpContext, usuario);
            return RedirectToPage("/Index");
        }

        public class LoginInput
        {
            public string? NombreUsuario { get; set; }
            public string? Contrasena { get; set; }
        }
    }
}

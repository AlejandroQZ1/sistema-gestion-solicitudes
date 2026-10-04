using AdministracionSoluciones.Data;
using AdministracionSoluciones.Filters;
using AdministracionSoluciones.Models;
using AdministracionSoluciones.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdministracionSoluciones.Controllers
{
    /// <summary>USR1 - Pantalla de login.</summary>
    [PermitirSinSesion]
    public class LoginController : Controller
    {
        public const string MensajeCredencialesIncorrectas = "Usuario y/o contraseña incorrectos.";
        private const int MaximoIntentos = 3;

        private readonly AppDbContext _db;
        private readonly CifradoService _cifrado;
        private readonly BitacoraService _bitacora;

        public LoginController(AppDbContext db, CifradoService cifrado, BitacoraService bitacora)
        {
            _db = db;
            _cifrado = cifrado;
            _bitacora = bitacora;
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (Sesion.EstaIniciada(HttpContext))
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Mensaje = TempData["MensajeLogin"];
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(LoginViewModel modelo)
        {
            // Sin usuario o sin contraseña
            if (string.IsNullOrWhiteSpace(modelo.NombreUsuario) || string.IsNullOrEmpty(modelo.Contrasena))
            {
                return Error(modelo);
            }

            var nombre = modelo.NombreUsuario.Trim();
            var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.NombreUsuario == nombre);

            // El usuario no existe
            if (usuario == null)
            {
                return Error(modelo);
            }

            // Bloqueado o inactivo: no puede entrar aunque la contraseña sea correcta
            if (usuario.Estado != EstadosUsuario.Activo)
            {
                return Error(modelo);
            }

            // Contraseña incorrecta: se suma un intento; al tercero se bloquea permanentemente
            if (!_cifrado.ContrasenaEsCorrecta(modelo.Contrasena, usuario.Contrasena))
            {
                usuario.IntentosFallidos++;
                if (usuario.IntentosFallidos >= MaximoIntentos)
                {
                    usuario.Estado = EstadosUsuario.Bloqueado;
                    _bitacora.Registrar(usuario.NombreUsuario, "Login", "Bloqueo por intentos fallidos",
                        BitacoraService.DatosUsuario(usuario));
                }

                await _db.SaveChangesAsync();
                return Error(modelo);
            }

            // Credenciales correctas
            usuario.IntentosFallidos = 0;
            await _db.SaveChangesAsync();

            Sesion.Iniciar(HttpContext, usuario);
            return RedirectToAction("Index", "Home");
        }

        public IActionResult CerrarSesion()
        {
            Sesion.Cerrar(HttpContext);
            return RedirectToAction(nameof(Index));
        }

        private IActionResult Error(LoginViewModel modelo)
        {
            ViewBag.Error = MensajeCredencialesIncorrectas;
            modelo.Contrasena = null;
            return View(modelo);
        }
    }
}

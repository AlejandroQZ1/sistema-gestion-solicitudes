using AdministracionSoluciones.Data;
using AdministracionSoluciones.Models;
using AdministracionSoluciones.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdministracionSoluciones.Controllers
{
    /// <summary>USR4 - Administración de usuarios: listar, crear, actualizar, eliminar y cambiar estado.</summary>
    public class UsuariosController : Controller
    {
        private const string Modulo = "Usuarios";
        public const string MensajeDatosRelacionados = "No se puede eliminar un registro con datos relacionados.";

        private readonly AppDbContext _db;
        private readonly CifradoService _cifrado;
        private readonly BitacoraService _bitacora;

        public UsuariosController(AppDbContext db, CifradoService cifrado, BitacoraService bitacora)
        {
            _db = db;
            _cifrado = cifrado;
            _bitacora = bitacora;
        }

        private string UsuarioActual => Sesion.NombreUsuario(HttpContext);

        // ---------- LISTAR ----------

        public async Task<IActionResult> Index()
        {
            var usuarios = await _db.Usuarios.OrderBy(u => u.NombreUsuario).ToListAsync();
            return View(usuarios);
        }

        // ---------- CREAR ----------

        [HttpGet]
        public IActionResult Crear()
        {
            return View("Formulario", new UsuarioFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(UsuarioFormViewModel modelo)
        {
            modelo.Id = 0;
            if (string.IsNullOrWhiteSpace(modelo.Contrasena))
            {
                ModelState.AddModelError(nameof(modelo.Contrasena), "La contraseña es requerida.");
            }
            await ValidarAsync(modelo);

            if (!ModelState.IsValid)
            {
                return View("Formulario", modelo);
            }

            var usuario = new Usuario
            {
                NombreUsuario = modelo.NombreUsuario.Trim(),
                NombreCompleto = modelo.NombreCompleto.Trim(),
                Correo = modelo.Correo.Trim(),
                Contrasena = _cifrado.Cifrar(modelo.Contrasena!),
                Estado = modelo.Estado,
                IntentosFallidos = 0
            };

            _db.Usuarios.Add(usuario);
            await _db.SaveChangesAsync(); // primero se guarda para obtener el Id

            _bitacora.Registrar(UsuarioActual, Modulo, "Crear", BitacoraService.DatosUsuario(usuario));
            await _db.SaveChangesAsync();

            TempData["Exito"] = $"El usuario \"{usuario.NombreUsuario}\" se creó correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- ACTUALIZAR ----------

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var usuario = await _db.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }

            return View("Formulario", new UsuarioFormViewModel
            {
                Id = usuario.Id,
                NombreUsuario = usuario.NombreUsuario,
                NombreCompleto = usuario.NombreCompleto,
                Correo = usuario.Correo,
                Estado = usuario.Estado
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, UsuarioFormViewModel modelo)
        {
            var usuario = await _db.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }

            modelo.Id = id;
            await ValidarAsync(modelo);
            if (id == Sesion.UsuarioId(HttpContext) && modelo.Estado != EstadosUsuario.Activo)
            {
                ModelState.AddModelError(nameof(modelo.Estado), "No puede inactivar ni bloquear su propio usuario.");
            }

            if (!ModelState.IsValid)
            {
                return View("Formulario", modelo);
            }

            usuario.NombreUsuario = modelo.NombreUsuario.Trim();
            usuario.NombreCompleto = modelo.NombreCompleto.Trim();
            usuario.Correo = modelo.Correo.Trim();
            CambiarEstado(usuario, modelo.Estado);
            if (!string.IsNullOrWhiteSpace(modelo.Contrasena))
            {
                usuario.Contrasena = _cifrado.Cifrar(modelo.Contrasena);
            }

            _bitacora.Registrar(UsuarioActual, Modulo, "Actualizar", BitacoraService.DatosUsuario(usuario));
            await _db.SaveChangesAsync();

            // Si el usuario se editó a sí mismo, se actualiza el nombre que aparece en el encabezado
            if (id == Sesion.UsuarioId(HttpContext))
            {
                Sesion.Iniciar(HttpContext, usuario);
            }

            TempData["Exito"] = $"El usuario \"{usuario.NombreUsuario}\" se actualizó correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- ELIMINAR ----------

        [HttpGet]
        public async Task<IActionResult> Eliminar(int id)
        {
            var usuario = await _db.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }

            return View(usuario);
        }

        [HttpPost, ActionName("Eliminar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarConfirmado(int id)
        {
            var usuario = await _db.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }

            if (id == Sesion.UsuarioId(HttpContext))
            {
                TempData["Error"] = "No puede eliminar su propio usuario.";
                return RedirectToAction(nameof(Index));
            }

            if (await TieneDatosRelacionadosAsync(id))
            {
                TempData["Error"] = MensajeDatosRelacionados;
                return RedirectToAction(nameof(Index));
            }

            var datos = BitacoraService.DatosUsuario(usuario);
            _db.Usuarios.Remove(usuario);
            _bitacora.Registrar(UsuarioActual, Modulo, "Eliminar", datos);

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Respaldo: si la base de datos tiene una llave foránea hacia este usuario, no deja borrarlo
                TempData["Error"] = MensajeDatosRelacionados;
                return RedirectToAction(nameof(Index));
            }

            TempData["Exito"] = $"El usuario \"{usuario.NombreUsuario}\" se eliminó correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- ACTIVAR / INACTIVAR / BLOQUEAR DESDE EL LISTADO ----------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(int id, string estado)
        {
            var usuario = await _db.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }

            if (!EstadosUsuario.Todos.Contains(estado))
            {
                TempData["Error"] = "El estado indicado no es válido.";
                return RedirectToAction(nameof(Index));
            }

            if (id == Sesion.UsuarioId(HttpContext) && estado != EstadosUsuario.Activo)
            {
                TempData["Error"] = "No puede inactivar ni bloquear su propio usuario.";
                return RedirectToAction(nameof(Index));
            }

            CambiarEstado(usuario, estado);
            _bitacora.Registrar(UsuarioActual, Modulo, "Cambio de estado", BitacoraService.DatosUsuario(usuario));
            await _db.SaveChangesAsync();

            TempData["Exito"] = $"El usuario \"{usuario.NombreUsuario}\" ahora está {estado.ToLower()}.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- AYUDAS ----------

        private async Task ValidarAsync(UsuarioFormViewModel modelo)
        {
            if (!EstadosUsuario.Todos.Contains(modelo.Estado))
            {
                ModelState.AddModelError(nameof(modelo.Estado), "El estado indicado no es válido.");
            }

            var nombre = modelo.NombreUsuario?.Trim();
            if (!string.IsNullOrEmpty(nombre)
                && await _db.Usuarios.AnyAsync(u => u.NombreUsuario == nombre && u.Id != modelo.Id))
            {
                ModelState.AddModelError(nameof(modelo.NombreUsuario), "Ya existe un usuario con ese nombre de usuario.");
            }
        }

        private static void CambiarEstado(Usuario usuario, string estado)
        {
            // Al reactivar un usuario bloqueado se reinician sus intentos fallidos
            if (estado == EstadosUsuario.Activo && usuario.Estado != EstadosUsuario.Activo)
            {
                usuario.IntentosFallidos = 0;
            }
            usuario.Estado = estado;
        }

        /// <summary>
        /// Indica si el usuario ya fue asignado en otra parte del sistema.
        /// PENDIENTE: cuando existan las tablas de los demás módulos, agregar aquí las validaciones, por ejemplo:
        ///   if (await _db.Solicitudes.AnyAsync(s => s.UsuarioId == id)) return true;
        ///   if (await _db.Tareas.AnyAsync(t => t.ResponsableId == id)) return true;
        /// </summary>
        private Task<bool> TieneDatosRelacionadosAsync(int id)
        {
            return Task.FromResult(false);
        }
    }
}

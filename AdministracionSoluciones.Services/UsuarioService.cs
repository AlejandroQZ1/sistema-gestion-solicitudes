using System.Data.Common;
using AdministracionSoluciones.Entities;
using AdministracionSoluciones.Repository;
using AdministracionSoluciones.Services.Abstract;

namespace AdministracionSoluciones.Services
{
    /// <summary>USR4 - Reglas de negocio de la administración de usuarios.</summary>
    public class UsuarioService : IUsuarioService
    {
        private const string Modulo = "Usuarios";
        public const string MensajeDatosRelacionados = "No se puede eliminar un registro con datos relacionados.";
        private const string MensajePropioUsuario = "No puede inactivar ni bloquear su propio usuario.";

        private readonly UsuarioRepository _usuarioRepository;
        private readonly ICifradoService _cifradoService;
        private readonly IBitacoraService _bitacoraService;

        public UsuarioService(UsuarioRepository usuarioRepository, ICifradoService cifradoService, IBitacoraService bitacoraService)
        {
            _usuarioRepository = usuarioRepository;
            _cifradoService = cifradoService;
            _bitacoraService = bitacoraService;
        }

        public Task<IEnumerable<Usuario>> GetAllAsync()
        {
            return _usuarioRepository.GetAllAsync();
        }

        public Task<Usuario?> GetByIdAsync(int id)
        {
            return _usuarioRepository.GetByIdAsync(id);
        }

        public async Task<Resultado> CrearAsync(Usuario usuario, string? contrasena, string usuarioActual)
        {
            if (string.IsNullOrWhiteSpace(contrasena))
            {
                return Resultado.Error("La contraseña es requerida.", nameof(Usuario.Contrasena));
            }

            Limpiar(usuario);
            var error = await ValidarAsync(usuario);
            if (error != null)
            {
                return error;
            }

            usuario.Contrasena = _cifradoService.Cifrar(contrasena);
            usuario.IntentosFallidos = 0;
            usuario.UsuarioID = await _usuarioRepository.InsertAsync(usuario);

            await _bitacoraService.RegistrarAsync(usuarioActual, Modulo, "Crear", BitacoraService.DatosUsuario(usuario));
            return Resultado.Ok($"El usuario \"{usuario.NombreUsuario}\" se creó correctamente.");
        }

        public async Task<Resultado> ActualizarAsync(Usuario datos, string? nuevaContrasena, int? idSesion, string usuarioActual)
        {
            var usuario = await _usuarioRepository.GetByIdAsync(datos.UsuarioID);
            if (usuario == null)
            {
                return Resultado.Error("El usuario no existe.");
            }

            Limpiar(datos);
            var error = await ValidarAsync(datos);
            if (error != null)
            {
                return error;
            }

            if (datos.UsuarioID == idSesion && datos.Estado != EstadosUsuario.Activo)
            {
                return Resultado.Error(MensajePropioUsuario, nameof(Usuario.Estado));
            }

            usuario.NombreUsuario = datos.NombreUsuario;
            usuario.NombreCompleto = datos.NombreCompleto;
            usuario.Correo = datos.Correo;
            AplicarEstado(usuario, datos.Estado);

            // Si la contraseña se deja en blanco, se mantiene la actual
            if (!string.IsNullOrWhiteSpace(nuevaContrasena))
            {
                usuario.Contrasena = _cifradoService.Cifrar(nuevaContrasena);
            }

            await _usuarioRepository.UpdateAsync(usuario);
            await _bitacoraService.RegistrarAsync(usuarioActual, Modulo, "Actualizar", BitacoraService.DatosUsuario(usuario));
            return Resultado.Ok($"El usuario \"{usuario.NombreUsuario}\" se actualizó correctamente.");
        }

        public async Task<Resultado> EliminarAsync(int id, int? idSesion, string usuarioActual)
        {
            var usuario = await _usuarioRepository.GetByIdAsync(id);
            if (usuario == null)
            {
                return Resultado.Error("El usuario no existe.");
            }

            if (id == idSesion)
            {
                return Resultado.Error("No puede eliminar su propio usuario.");
            }

            if (await _usuarioRepository.TieneDatosRelacionadosAsync(id))
            {
                return Resultado.Error(MensajeDatosRelacionados);
            }

            try
            {
                await _usuarioRepository.DeleteAsync(id);
            }
            catch (DbException)
            {
                // Respaldo: si la base de datos tiene una llave foránea hacia este usuario, no deja borrarlo
                return Resultado.Error(MensajeDatosRelacionados);
            }

            await _bitacoraService.RegistrarAsync(usuarioActual, Modulo, "Eliminar", BitacoraService.DatosUsuario(usuario));
            return Resultado.Ok($"El usuario \"{usuario.NombreUsuario}\" se eliminó correctamente.");
        }

        public async Task<Resultado> CambiarEstadoAsync(int id, string estado, int? idSesion, string usuarioActual)
        {
            var usuario = await _usuarioRepository.GetByIdAsync(id);
            if (usuario == null)
            {
                return Resultado.Error("El usuario no existe.");
            }

            if (!EstadosUsuario.Todos.Contains(estado))
            {
                return Resultado.Error("El estado indicado no es válido.");
            }

            if (id == idSesion && estado != EstadosUsuario.Activo)
            {
                return Resultado.Error(MensajePropioUsuario);
            }

            AplicarEstado(usuario, estado);
            await _usuarioRepository.UpdateEstadoAsync(usuario.UsuarioID, usuario.Estado, usuario.IntentosFallidos);
            await _bitacoraService.RegistrarAsync(usuarioActual, Modulo, "Cambio de estado", BitacoraService.DatosUsuario(usuario));
            return Resultado.Ok($"El usuario \"{usuario.NombreUsuario}\" ahora está {estado.ToLower()}.");
        }

        public async Task CrearAdministradorInicialAsync(string nombreUsuario, string nombreCompleto, string correo, string contrasena)
        {
            if (await _usuarioRepository.CountAsync() > 0)
            {
                return;
            }

            await _usuarioRepository.InsertAsync(new Usuario
            {
                NombreUsuario = nombreUsuario,
                NombreCompleto = nombreCompleto,
                Correo = correo,
                Contrasena = _cifradoService.Cifrar(contrasena),
                Estado = EstadosUsuario.Activo,
                IntentosFallidos = 0
            });
        }

        // ---------- AYUDAS ----------

        private async Task<Resultado?> ValidarAsync(Usuario usuario)
        {
            if (!EstadosUsuario.Todos.Contains(usuario.Estado))
            {
                return Resultado.Error("El estado indicado no es válido.", nameof(Usuario.Estado));
            }

            if (await _usuarioRepository.ExisteNombreUsuarioAsync(usuario.NombreUsuario, usuario.UsuarioID))
            {
                return Resultado.Error("Ya existe un usuario con ese nombre de usuario.", nameof(Usuario.NombreUsuario));
            }

            return null;
        }

        private static void Limpiar(Usuario usuario)
        {
            usuario.NombreUsuario = usuario.NombreUsuario?.Trim() ?? string.Empty;
            usuario.NombreCompleto = usuario.NombreCompleto?.Trim() ?? string.Empty;
            usuario.Correo = usuario.Correo?.Trim() ?? string.Empty;
        }

        /// <summary>Al reactivar un usuario bloqueado o inactivo se reinician sus intentos fallidos.</summary>
        private static void AplicarEstado(Usuario usuario, string estado)
        {
            if (estado == EstadosUsuario.Activo && usuario.Estado != EstadosUsuario.Activo)
            {
                usuario.IntentosFallidos = 0;
            }
            usuario.Estado = estado;
        }
    }
}

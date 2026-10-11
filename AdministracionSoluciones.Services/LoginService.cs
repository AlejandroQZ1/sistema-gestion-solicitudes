using AdministracionSoluciones.Entities;
using AdministracionSoluciones.Repository;
using AdministracionSoluciones.Services.Abstract;

namespace AdministracionSoluciones.Services
{
    /// <summary>USR1 - Validación de credenciales y bloqueo por intentos fallidos.</summary>
    public class LoginService : ILoginService
    {
        private const int MaximoIntentos = 3;

        private readonly UsuarioRepository _usuarioRepository;
        private readonly ICifradoService _cifradoService;
        private readonly IBitacoraService _bitacoraService;

        public LoginService(UsuarioRepository usuarioRepository, ICifradoService cifradoService, IBitacoraService bitacoraService)
        {
            _usuarioRepository = usuarioRepository;
            _cifradoService = cifradoService;
            _bitacoraService = bitacoraService;
        }

        public async Task<Usuario?> IniciarSesionAsync(string? nombreUsuario, string? contrasena)
        {
            // Sin usuario o sin contraseña
            if (string.IsNullOrWhiteSpace(nombreUsuario) || string.IsNullOrEmpty(contrasena))
            {
                return null;
            }

            var usuario = await _usuarioRepository.GetByNombreUsuarioAsync(nombreUsuario.Trim());

            // No existe, o está bloqueado / inactivo (no entra aunque la contraseña sea correcta)
            if (usuario == null || usuario.Estado != EstadosUsuario.Activo)
            {
                return null;
            }

            // Contraseña incorrecta: suma un intento; al tercero se bloquea permanentemente
            if (!_cifradoService.ContrasenaEsCorrecta(contrasena, usuario.Contrasena))
            {
                usuario.IntentosFallidos++;
                if (usuario.IntentosFallidos >= MaximoIntentos)
                {
                    usuario.Estado = EstadosUsuario.Bloqueado;
                    await _bitacoraService.RegistrarAsync(usuario.NombreUsuario, "Login", "Bloqueo por intentos fallidos",
                        BitacoraService.DatosUsuario(usuario));
                }

                await _usuarioRepository.UpdateEstadoAsync(usuario.UsuarioID, usuario.Estado, usuario.IntentosFallidos);
                return null;
            }

            // Credenciales correctas: se reinician los intentos
            if (usuario.IntentosFallidos != 0)
            {
                usuario.IntentosFallidos = 0;
                await _usuarioRepository.UpdateEstadoAsync(usuario.UsuarioID, usuario.Estado, 0);
            }

            return usuario;
        }
    }
}

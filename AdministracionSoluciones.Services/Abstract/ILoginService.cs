using AdministracionSoluciones.Entities;

namespace AdministracionSoluciones.Services.Abstract
{
    public interface ILoginService
    {
        /// <summary>
        /// Valida usuario y contraseña (USR1). Devuelve el usuario si son correctos, o null si no.
        /// Al tercer intento fallido bloquea al usuario.
        /// </summary>
        Task<Usuario?> IniciarSesionAsync(string? nombreUsuario, string? contrasena);
    }
}

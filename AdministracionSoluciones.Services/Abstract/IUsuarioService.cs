using AdministracionSoluciones.Entities;

namespace AdministracionSoluciones.Services.Abstract
{
    /// <summary>
    /// Administración de usuarios (USR4).
    /// "usuarioActual" es el nombre de quien hace la acción (para la bitácora) y
    /// "idSesion" es el Id del usuario con sesión iniciada (no puede eliminarse ni bloquearse a sí mismo).
    /// </summary>
    public interface IUsuarioService
    {
        Task<IEnumerable<Usuario>> GetAllAsync();
        Task<Usuario?> GetByIdAsync(int id);
        Task<Resultado> CrearAsync(Usuario usuario, string? contrasena, string usuarioActual);
        Task<Resultado> ActualizarAsync(Usuario datos, string? nuevaContrasena, int? idSesion, string usuarioActual);
        Task<Resultado> EliminarAsync(int id, int? idSesion, string usuarioActual);
        Task<Resultado> CambiarEstadoAsync(int id, string estado, int? idSesion, string usuarioActual);

        /// <summary>Crea el usuario administrador si la tabla de usuarios está vacía.</summary>
        Task CrearAdministradorInicialAsync(string nombreUsuario, string nombreCompleto, string correo, string contrasena);
    }
}

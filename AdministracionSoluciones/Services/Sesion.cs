using AdministracionSoluciones.Models;

namespace AdministracionSoluciones.Services
{
    /// <summary>
    /// Ayuda para leer y guardar los datos del usuario en sesión (USR1).
    /// Desde cualquier controlador: Sesion.NombreUsuario(HttpContext)
    /// Desde cualquier vista: Sesion.NombreCompleto(Context)
    /// </summary>
    public static class Sesion
    {
        private const string ClaveId = "UsuarioId";
        private const string ClaveNombreUsuario = "NombreUsuario";
        private const string ClaveNombreCompleto = "NombreCompleto";

        public static void Iniciar(HttpContext http, Usuario usuario)
        {
            http.Session.SetInt32(ClaveId, usuario.Id);
            http.Session.SetString(ClaveNombreUsuario, usuario.NombreUsuario);
            http.Session.SetString(ClaveNombreCompleto, usuario.NombreCompleto);
        }

        public static void Cerrar(HttpContext http) => http.Session.Clear();

        public static bool EstaIniciada(HttpContext http) => http.Session.GetInt32(ClaveId).HasValue;

        public static int? UsuarioId(HttpContext http) => http.Session.GetInt32(ClaveId);

        public static string NombreUsuario(HttpContext http) => http.Session.GetString(ClaveNombreUsuario) ?? string.Empty;

        public static string NombreCompleto(HttpContext http) => http.Session.GetString(ClaveNombreCompleto) ?? string.Empty;
    }
}

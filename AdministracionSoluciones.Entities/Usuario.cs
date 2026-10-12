namespace AdministracionSoluciones.Entities
{
    /// <summary>
    /// Usuario del sistema (USR1, USR4). Tabla "usuarios" en MySQL.
    /// </summary>
    public class Usuario
    {
        public int UsuarioID { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;

        /// <summary>Contraseña cifrada con AES-GCM 256 (texto en Base64).</summary>
        public string Contrasena { get; set; } = string.Empty;

        /// <summary>Activo, Inactivo o Bloqueado (ver <see cref="EstadosUsuario"/>).</summary>
        public string Estado { get; set; } = EstadosUsuario.Activo;

        /// <summary>Intentos de login fallidos seguidos. Al llegar a 3 el usuario se bloquea.</summary>
        public int IntentosFallidos { get; set; }
    }
}

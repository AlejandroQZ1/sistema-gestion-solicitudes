using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdministracionSoluciones.Models
{
    /// <summary>
    /// Usuario del sistema (USR1, USR4).
    /// Si la base de datos del equipo usa otros nombres de tabla o columnas,
    /// solo hay que ajustar los atributos [Table] y [Column] de esta clase.
    /// </summary>
    [Table("Usuarios")]
    public class Usuario
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        [Column("NombreUsuario")]
        public string NombreUsuario { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        [Column("NombreCompleto")]
        public string NombreCompleto { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        [Column("Correo")]
        public string Correo { get; set; } = string.Empty;

        /// <summary>Contraseña cifrada con AES-GCM 256 (texto en Base64).</summary>
        [Required, MaxLength(500)]
        [Column("Contrasena")]
        public string Contrasena { get; set; } = string.Empty;

        /// <summary>Activo, Inactivo o Bloqueado (ver <see cref="EstadosUsuario"/>).</summary>
        [Required, MaxLength(20)]
        [Column("Estado")]
        public string Estado { get; set; } = EstadosUsuario.Activo;

        /// <summary>Intentos de login fallidos seguidos. Al llegar a 3 el usuario se bloquea.</summary>
        [Column("IntentosFallidos")]
        public int IntentosFallidos { get; set; }
    }

    public static class EstadosUsuario
    {
        public const string Activo = "Activo";
        public const string Inactivo = "Inactivo";
        public const string Bloqueado = "Bloqueado";

        public static readonly string[] Todos = { Activo, Inactivo, Bloqueado };
    }
}

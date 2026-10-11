using System.ComponentModel.DataAnnotations;
using AdministracionSoluciones.Entities;

namespace AdministracionSoluciones.Pages.Usuarios
{
    /// <summary>Datos del formulario de crear / editar usuario (USR4). Todos los datos son requeridos.</summary>
    public class UsuarioInput
    {
        public int UsuarioID { get; set; }

        [Display(Name = "Nombre de usuario")]
        [Required(ErrorMessage = "El nombre de usuario es requerido.")]
        [MaxLength(50, ErrorMessage = "Máximo 50 caracteres.")]
        public string NombreUsuario { get; set; } = string.Empty;

        [Display(Name = "Nombre completo")]
        [Required(ErrorMessage = "El nombre completo es requerido.")]
        [MaxLength(150, ErrorMessage = "Máximo 150 caracteres.")]
        public string NombreCompleto { get; set; } = string.Empty;

        [Display(Name = "Correo")]
        [Required(ErrorMessage = "El correo es requerido.")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        [MaxLength(150, ErrorMessage = "Máximo 150 caracteres.")]
        public string Correo { get; set; } = string.Empty;

        /// <summary>Requerida al crear. Al editar, si se deja vacía se mantiene la contraseña actual.</summary>
        [Display(Name = "Contraseña")]
        [DataType(DataType.Password)]
        [MaxLength(100, ErrorMessage = "Máximo 100 caracteres.")]
        public string? Contrasena { get; set; }

        [Display(Name = "Estado")]
        [Required(ErrorMessage = "El estado es requerido.")]
        public string Estado { get; set; } = EstadosUsuario.Activo;

        public bool EsNuevo => UsuarioID == 0;

        public Usuario ToUsuario() => new()
        {
            UsuarioID = UsuarioID,
            NombreUsuario = NombreUsuario,
            NombreCompleto = NombreCompleto,
            Correo = Correo,
            Estado = Estado
        };

        public static UsuarioInput FromUsuario(Usuario u) => new()
        {
            UsuarioID = u.UsuarioID,
            NombreUsuario = u.NombreUsuario,
            NombreCompleto = u.NombreCompleto,
            Correo = u.Correo,
            Estado = u.Estado
        };
    }
}

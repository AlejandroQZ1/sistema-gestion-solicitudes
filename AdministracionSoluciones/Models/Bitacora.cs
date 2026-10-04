using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdministracionSoluciones.Models
{
    /// <summary>
    /// Registro de bitácora. Ajustar a lo que indique la sección
    /// "Manejo de bitácoras" del enunciado si pide otros campos.
    /// </summary>
    [Table("Bitacora")]
    public class Bitacora
    {
        [Key]
        public int Id { get; set; }

        public DateTime Fecha { get; set; }

        /// <summary>Nombre de usuario de quien hizo la acción.</summary>
        [Required, MaxLength(50)]
        public string Usuario { get; set; } = string.Empty;

        /// <summary>Pantalla o tabla afectada, por ejemplo "Usuarios".</summary>
        [Required, MaxLength(50)]
        public string Modulo { get; set; } = string.Empty;

        /// <summary>Crear, Actualizar, Eliminar, Cambio de estado, Bloqueo...</summary>
        [Required, MaxLength(50)]
        public string Accion { get; set; } = string.Empty;

        /// <summary>Datos del registro en formato JSON. Nunca incluye la contraseña.</summary>
        public string? Detalle { get; set; }
    }
}

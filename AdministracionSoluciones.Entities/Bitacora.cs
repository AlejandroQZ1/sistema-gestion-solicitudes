namespace AdministracionSoluciones.Entities
{
    /// <summary>
    /// Registro de bitácora. Tabla "bitacora" en MySQL.
    /// Ajustar a lo que indique la sección "Manejo de bitácoras" del enunciado si pide otros campos.
    /// </summary>
    public class Bitacora
    {
        public int BitacoraID { get; set; }
        public DateTime Fecha { get; set; }

        /// <summary>Nombre de usuario de quien hizo la acción.</summary>
        public string Usuario { get; set; } = string.Empty;

        /// <summary>Pantalla o tabla afectada, por ejemplo "Usuarios".</summary>
        public string Modulo { get; set; } = string.Empty;

        /// <summary>Crear, Actualizar, Eliminar, Cambio de estado, Bloqueo...</summary>
        public string Accion { get; set; } = string.Empty;

        /// <summary>Datos del registro en formato JSON. Nunca incluye la contraseña.</summary>
        public string? Detalle { get; set; }
    }
}

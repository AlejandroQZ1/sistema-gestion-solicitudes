using System.Text.Encodings.Web;
using System.Text.Json;
using AdministracionSoluciones.Entities;
using AdministracionSoluciones.Repository;
using AdministracionSoluciones.Services.Abstract;
using Microsoft.Extensions.Logging;

namespace AdministracionSoluciones.Services
{
    /// <summary>
    /// Guarda registros en la bitácora. Lo pueden usar todos los módulos del sistema.
    /// Los fallos se registran en el log de la aplicación para que no se confundan
    /// con fallos de la operación principal que originó la auditoría.
    /// </summary>
    public class BitacoraService : IBitacoraService
    {
        // Guarda las tildes y la ñ tal cual (sin códigos \u00E9).
        private static readonly JsonSerializerOptions OpcionesJson = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private readonly BitacoraRepository _bitacoraRepository;
        private readonly ILogger<BitacoraService> _logger;

        public BitacoraService(
            BitacoraRepository bitacoraRepository,
            ILogger<BitacoraService> logger)
        {
            _bitacoraRepository = bitacoraRepository;
            _logger = logger;
        }

        public async Task RegistrarAsync(
            string usuario,
            string modulo,
            string accion,
            object? datos = null)
        {
            var bitacora = new Bitacora
            {
                Fecha = DateTime.Now,
                Usuario = string.IsNullOrWhiteSpace(usuario) ? "sistema" : usuario,
                Modulo = modulo,
                Accion = accion,
                Detalle = datos == null
                    ? null
                    : JsonSerializer.Serialize(datos, OpcionesJson)
            };

            try
            {
                await _bitacoraRepository.InsertAsync(bitacora);
            }
            catch (Exception ex)
            {
                // La operación principal pudo haberse completado antes de intentar
                // registrar la auditoría. Dejamos evidencia técnica en el log.
                _logger.LogError(
                    ex,
                    "No se pudo registrar la bitácora. Módulo: {Modulo}; Acción: {Accion}; Usuario: {Usuario}",
                    modulo,
                    accion,
                    bitacora.Usuario);

                // No propagamos esta excepción para evitar informar que la operación
                // principal falló cuando ya pudo haberse guardado correctamente.
                // El fallo queda registrado en el log de la aplicación.
            }
        }

        /// <summary>Datos de un usuario aptos para la bitácora (sin contraseña).</summary>
        public static object DatosUsuario(Usuario u) => new
        {
            u.UsuarioID,
            u.NombreUsuario,
            u.NombreCompleto,
            u.Correo,
            u.Estado
        };
    }
}

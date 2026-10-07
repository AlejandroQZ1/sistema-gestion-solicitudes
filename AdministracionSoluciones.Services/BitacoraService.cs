using System.Text.Encodings.Web;
using System.Text.Json;
using AdministracionSoluciones.Entities;
using AdministracionSoluciones.Repository;
using AdministracionSoluciones.Services.Abstract;

namespace AdministracionSoluciones.Services
{
    /// <summary>Guarda registros en la bitácora. Lo pueden usar todos los módulos del sistema.</summary>
    public class BitacoraService : IBitacoraService
    {
        // Guarda las tildes y la ñ tal cual (sin códigos \u00E9)
        private static readonly JsonSerializerOptions OpcionesJson = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private readonly BitacoraRepository _bitacoraRepository;

        public BitacoraService(BitacoraRepository bitacoraRepository)
        {
            _bitacoraRepository = bitacoraRepository;
        }

        public Task RegistrarAsync(string usuario, string modulo, string accion, object? datos = null)
        {
            return _bitacoraRepository.InsertAsync(new Bitacora
            {
                Fecha = DateTime.Now,
                Usuario = string.IsNullOrWhiteSpace(usuario) ? "sistema" : usuario,
                Modulo = modulo,
                Accion = accion,
                Detalle = datos == null ? null : JsonSerializer.Serialize(datos, OpcionesJson)
            });
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

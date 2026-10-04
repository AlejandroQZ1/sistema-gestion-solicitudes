using System.Text.Json;
using AdministracionSoluciones.Data;
using AdministracionSoluciones.Models;

namespace AdministracionSoluciones.Services
{
    /// <summary>
    /// Guarda registros en la bitácora. Lo pueden usar todos los módulos del sistema.
    /// Importante: nunca pasar contraseñas dentro de "datos".
    /// </summary>
    public class BitacoraService
    {
        private readonly AppDbContext _db;

        public BitacoraService(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Agrega el registro a la bitácora. Se guarda junto con el siguiente SaveChangesAsync().
        /// </summary>
        public void Registrar(string usuario, string modulo, string accion, object? datos = null)
        {
            _db.Bitacora.Add(new Bitacora
            {
                Fecha = DateTime.Now,
                Usuario = string.IsNullOrWhiteSpace(usuario) ? "sistema" : usuario,
                Modulo = modulo,
                Accion = accion,
                Detalle = datos == null ? null : JsonSerializer.Serialize(datos)
            });
        }

        /// <summary>Datos de un usuario aptos para bitácora (sin contraseña).</summary>
        public static object DatosUsuario(Usuario u) => new
        {
            u.Id,
            u.NombreUsuario,
            u.NombreCompleto,
            u.Correo,
            u.Estado
        };
    }
}

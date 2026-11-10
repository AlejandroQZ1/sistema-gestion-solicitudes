using System.Data;

namespace AdministracionSoluciones.Services.Abstract
{
    public interface IBitacoraService
    {
        /// <summary>Guarda un registro en la bitácora. Nunca pasar contraseñas dentro de "datos".</summary>
        Task RegistrarAsync(string usuario, string modulo, string accion, object? datos = null);

        /// <summary>
        /// Registra usando la misma conexión y transacción de la operación auditada.
        /// Propaga errores para que la transacción pueda revertirse.
        /// </summary>
        Task RegistrarAsync(
            string usuario, string modulo, string accion, object? datos,
            IDbConnection connection, IDbTransaction transaction);
    }
}

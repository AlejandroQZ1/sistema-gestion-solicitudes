using AdministracionSoluciones.Entities;

namespace AdministracionSoluciones.Services.Abstract
{
    public interface IDesgloseService
    {
        Task<IEnumerable<Desglose>> GetAllAsync(int? solicitudId = null, string? usuarioActual = null);
        Task<Desglose?> GetByIdAsync(int id, string? usuarioActual = null);
        Task<Resultado> CrearAsync(Desglose desglose, string usuarioActual);
        Task<Resultado> ActualizarAsync(Desglose desglose, string usuarioActual);
        Task<Resultado> EliminarAsync(int id, string usuarioActual);
        Task<IEnumerable<Solicitud>> GetSolicitudesParaComboAsync();
        Task<Solicitud?> GetSolicitudByIdAsync(int solicitudId);
    }
}

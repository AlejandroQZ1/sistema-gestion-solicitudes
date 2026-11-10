using AdministracionSoluciones.Entities.Propuestas;
using AdministracionSoluciones.Entities;

namespace AdministracionSoluciones.Services.Abstract;

public interface IPropuestaService
{
    Task<IReadOnlyList<PropuestaResumen>> ListarPorSolicitudAsync(int solicitudId);
    Task<PropuestaDetalle?> ObtenerDetalleAsync(int solicitudId, int propuestaId);
    Task<AprobacionPropuesta?> ObtenerAprobacionAsync(int solicitudId);
    Task<IReadOnlyList<DesglosePropuestaHistorial>> ListarHistorialAsync(int solicitudId);
    Task<Resultado> AprobarAsync(int solicitudId, int propuestaId, string usuarioActual);
}

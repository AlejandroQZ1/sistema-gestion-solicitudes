using System.Data;
using AdministracionSoluciones.Entities.Propuestas;

namespace AdministracionSoluciones.Repository;

public interface IPropuestaRepository
{
    Task<IReadOnlyList<PropuestaResumen>> ListarPorSolicitudAsync(int solicitudId);
    Task<PropuestaDetalle?> ObtenerDetalleAsync(int solicitudId, int propuestaId);
    Task<AprobacionPropuesta?> ObtenerAprobacionAsync(int solicitudId);
    Task<IReadOnlyList<DesglosePropuestaHistorial>> ListarHistorialAsync(int solicitudId);

    Task<bool> BloquearSolicitudAsync(IDbConnection connection, IDbTransaction transaction, int solicitudId);
    Task<bool> BloquearPropuestaAsync(
        IDbConnection connection, IDbTransaction transaction, int solicitudId, int propuestaId);
    Task<PropuestaDetalle?> ObtenerDetalleAsync(
        IDbConnection connection, IDbTransaction transaction, int solicitudId, int propuestaId);
    Task<bool> TieneAprobacionAsync(IDbConnection connection, IDbTransaction transaction, int solicitudId);
    Task<IReadOnlyList<DesgloseAnterior>> ListarDesglosesVigentesAsync(
        IDbConnection connection, IDbTransaction transaction, int solicitudId);
    Task<int> EliminarDesglosesVigentesAsync(
        IDbConnection connection, IDbTransaction transaction, int solicitudId);
    Task<int> InsertarDesglosesPropuestosAsync(
        IDbConnection connection, IDbTransaction transaction, int solicitudId, int propuestaId);
    Task<IReadOnlyList<DesgloseAnterior>> ListarDesglosesVigentesTrasAprobacionAsync(
        IDbConnection connection, IDbTransaction transaction, int solicitudId);
    Task<int> RegistrarAprobacionAsync(
        IDbConnection connection, IDbTransaction transaction, AprobacionPropuesta aprobacion);
    Task<int> ArchivarDesglosesAsync(
        IDbConnection connection, IDbTransaction transaction, int aprobacionId,
        IReadOnlyList<DesgloseAnterior> desgloses);
    Task<int> MarcarAprobadaAsync(
        IDbConnection connection, IDbTransaction transaction, int solicitudId, int propuestaId);
}

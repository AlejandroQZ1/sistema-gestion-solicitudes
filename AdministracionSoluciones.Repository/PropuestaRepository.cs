using System.Data;
using AdministracionSoluciones.Entities.Propuestas;
using Dapper;

namespace AdministracionSoluciones.Repository;

/// <summary>Lectura de propuestas y operaciones SQL de la aprobación EXT7.</summary>
public sealed class PropuestaRepository : IPropuestaRepository
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public PropuestaRepository(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<IReadOnlyList<PropuestaResumen>> ListarPorSolicitudAsync(int solicitudId)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        const string sql = @"
            SELECT p.PropuestaID, p.SolicitudID, p.ProveedorID,
                   v.CedulaJuridica, v.Nombre AS NombreProveedor,
                   p.FechaPropuesta, p.Aprobada,
                   CASE WHEN EXISTS (
                       SELECT 1 FROM aprobaciones_propuesta ap
                       WHERE ap.SolicitudID = p.SolicitudID
                   ) OR EXISTS (
                       SELECT 1 FROM propuestas pa
                       WHERE pa.SolicitudID = p.SolicitudID AND pa.Aprobada = 1
                   ) THEN 1 ELSE 0 END AS SolicitudTieneAprobacion
            FROM propuestas p
            INNER JOIN proveedores v ON v.ProveedorID = p.ProveedorID
            WHERE p.SolicitudID = @SolicitudID
            ORDER BY p.FechaPropuesta DESC, p.PropuestaID DESC;";
        var result = await connection.QueryAsync<PropuestaResumen>(sql, new { SolicitudID = solicitudId });
        return result.AsList();
    }

    public async Task<PropuestaDetalle?> ObtenerDetalleAsync(int solicitudId, int propuestaId)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var detail = await ObtenerDetalleCoreAsync(connection, null, solicitudId, propuestaId, bloquear: false);
        if (detail == null) return null;

        var lines = await connection.QueryAsync<DesglosePropuesta>(@"
            SELECT DesglosePropuestaID, PropuestaID, Mes, Anio, Horas, Monto,
                   IVA, Total, Observaciones, PorcentajeCobro
            FROM desgloses_propuesta
            WHERE PropuestaID = @PropuestaID
            ORDER BY Anio, Mes, DesglosePropuestaID;", new { PropuestaID = propuestaId });
        detail.Lineas = lines.AsList();
        return detail;
    }

    public async Task<AprobacionPropuesta?> ObtenerAprobacionAsync(int solicitudId)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<AprobacionPropuesta>(@"
            SELECT ap.AprobacionID, ap.SolicitudID, ap.PropuestaID,
                   p.ProveedorID, v.CedulaJuridica, v.Nombre AS NombreProveedor,
                   ap.Usuario, ap.FechaAprobacion,
                   ap.DatosAnterioresJson, ap.DatosNuevosJson
            FROM aprobaciones_propuesta ap
            INNER JOIN propuestas p ON p.PropuestaID = ap.PropuestaID
                                     AND p.SolicitudID = ap.SolicitudID
            INNER JOIN proveedores v ON v.ProveedorID = p.ProveedorID
            WHERE ap.SolicitudID = @SolicitudID;", new { SolicitudID = solicitudId });
    }

    public async Task<IReadOnlyList<DesglosePropuestaHistorial>> ListarHistorialAsync(int solicitudId)
    {
        using var connection = _dbConnectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<DesglosePropuestaHistorial>(@"
            SELECT h.HistorialDesgloseID, h.AprobacionID, h.DesgloseID,
                   h.SolicitudID, h.DesglosePropuestaID, h.PropuestaID,
                   h.Mes, h.Anio, h.Horas, h.Monto, h.IVA, h.Total,
                   h.Observaciones, h.PorcentajeCobro
            FROM historial_desgloses h
            INNER JOIN aprobaciones_propuesta ap ON ap.AprobacionID = h.AprobacionID
            WHERE ap.SolicitudID = @SolicitudID
            ORDER BY h.HistorialDesgloseID;", new { SolicitudID = solicitudId });
        return rows.AsList();
    }

    public async Task<bool> BloquearSolicitudAsync(
        IDbConnection connection, IDbTransaction transaction, int solicitudId)
    {
        var id = await connection.QuerySingleOrDefaultAsync<int?>(@"
            SELECT SolicitudID FROM solicitudes
            WHERE SolicitudID = @SolicitudID FOR UPDATE;",
            new { SolicitudID = solicitudId }, transaction);
        return id.HasValue;
    }

    public async Task<bool> BloquearPropuestaAsync(
        IDbConnection connection, IDbTransaction transaction, int solicitudId, int propuestaId)
    {
        var id = await connection.QuerySingleOrDefaultAsync<int?>(@"
            SELECT PropuestaID FROM propuestas
            WHERE SolicitudID = @SolicitudID AND PropuestaID = @PropuestaID FOR UPDATE;",
            new { SolicitudID = solicitudId, PropuestaID = propuestaId }, transaction);
        return id.HasValue;
    }

    public async Task<PropuestaDetalle?> ObtenerDetalleAsync(
        IDbConnection connection, IDbTransaction transaction, int solicitudId, int propuestaId)
    {
        var detail = await ObtenerDetalleCoreAsync(connection, transaction, solicitudId, propuestaId, bloquear: false);
        if (detail == null) return null;

        var lines = await connection.QueryAsync<DesglosePropuesta>(@"
            SELECT DesglosePropuestaID, PropuestaID, Mes, Anio, Horas, Monto,
                   IVA, Total, Observaciones, PorcentajeCobro
            FROM desgloses_propuesta
            WHERE PropuestaID = @PropuestaID
            ORDER BY Anio, Mes, DesglosePropuestaID FOR UPDATE;",
            new { PropuestaID = propuestaId }, transaction);
        detail.Lineas = lines.AsList();
        return detail;
    }

    public async Task<bool> TieneAprobacionAsync(
        IDbConnection connection, IDbTransaction transaction, int solicitudId)
    {
        return await connection.ExecuteScalarAsync<bool>(@"
            SELECT EXISTS (
                SELECT 1 FROM aprobaciones_propuesta WHERE SolicitudID = @SolicitudID
            ) OR EXISTS (
                SELECT 1 FROM propuestas WHERE SolicitudID = @SolicitudID AND Aprobada = 1
            );", new { SolicitudID = solicitudId }, transaction);
    }

    public async Task<IReadOnlyList<DesgloseAnterior>> ListarDesglosesVigentesAsync(
        IDbConnection connection, IDbTransaction transaction, int solicitudId)
    {
        var rows = await connection.QueryAsync<DesgloseAnterior>(@"
            SELECT DesgloseID, SolicitudID, DesglosePropuestaID, PropuestaID,
                   Mes, Anio, Horas, Monto, IVA, Total, Observaciones, PorcentajeCobro
            FROM desgloses
            WHERE SolicitudID = @SolicitudID
            ORDER BY DesgloseID FOR UPDATE;", new { SolicitudID = solicitudId }, transaction);
        return rows.AsList();
    }

    public Task<int> EliminarDesglosesVigentesAsync(
        IDbConnection connection, IDbTransaction transaction, int solicitudId)
    {
        return connection.ExecuteAsync(
            "DELETE FROM desgloses WHERE SolicitudID = @SolicitudID;",
            new { SolicitudID = solicitudId }, transaction);
    }

    public Task<int> InsertarDesglosesPropuestosAsync(
        IDbConnection connection, IDbTransaction transaction, int solicitudId, int propuestaId)
    {
        const string sql = @"
            INSERT INTO desgloses
                (SolicitudID, DesglosePropuestaID, PropuestaID, Mes, Anio,
                 Horas, Monto, Observaciones, PorcentajeCobro)
            SELECT @SolicitudID, dp.DesglosePropuestaID, p.PropuestaID,
                   dp.Mes, dp.Anio, dp.Horas, dp.Monto, dp.Observaciones, dp.PorcentajeCobro
            FROM propuestas p
            INNER JOIN desgloses_propuesta dp ON dp.PropuestaID = p.PropuestaID
            WHERE p.PropuestaID = @PropuestaID AND p.SolicitudID = @SolicitudID;";
        return connection.ExecuteAsync(sql,
            new { SolicitudID = solicitudId, PropuestaID = propuestaId }, transaction);
    }

    public async Task<IReadOnlyList<DesgloseAnterior>> ListarDesglosesVigentesTrasAprobacionAsync(
        IDbConnection connection, IDbTransaction transaction, int solicitudId)
    {
        var rows = await connection.QueryAsync<DesgloseAnterior>(@"
            SELECT DesgloseID, SolicitudID, DesglosePropuestaID, PropuestaID,
                   Mes, Anio, Horas, Monto, IVA, Total, Observaciones, PorcentajeCobro
            FROM desgloses
            WHERE SolicitudID = @SolicitudID
            ORDER BY DesgloseID;", new { SolicitudID = solicitudId }, transaction);
        return rows.AsList();
    }

    public async Task<int> RegistrarAprobacionAsync(
        IDbConnection connection, IDbTransaction transaction, AprobacionPropuesta aprobacion)
    {
        const string sql = @"
            INSERT INTO aprobaciones_propuesta
                (SolicitudID, PropuestaID, Usuario, FechaAprobacion,
                 DatosAnterioresJson, DatosNuevosJson)
            VALUES
                (@SolicitudID, @PropuestaID, @Usuario, @FechaAprobacion,
                 @DatosAnterioresJson, @DatosNuevosJson);
            SELECT LAST_INSERT_ID();";
        return await connection.ExecuteScalarAsync<int>(sql, aprobacion, transaction);
    }

    public async Task<int> ArchivarDesglosesAsync(
        IDbConnection connection, IDbTransaction transaction, int aprobacionId,
        IReadOnlyList<DesgloseAnterior> desgloses)
    {
        if (desgloses.Count == 0) return 0;
        const string sql = @"
            INSERT INTO historial_desgloses
                (AprobacionID, DesgloseID, SolicitudID, DesglosePropuestaID, PropuestaID,
                 Mes, Anio, Horas, Monto, IVA, Total, Observaciones, PorcentajeCobro)
            VALUES
                (@AprobacionID, @DesgloseID, @SolicitudID, @DesglosePropuestaID, @PropuestaID,
                 @Mes, @Anio, @Horas, @Monto, @IVA, @Total, @Observaciones, @PorcentajeCobro);";
        return await connection.ExecuteAsync(sql,
            desgloses.Select(d => new
            {
                AprobacionID = aprobacionId,
                d.DesgloseID,
                d.SolicitudID,
                d.DesglosePropuestaID,
                d.PropuestaID,
                d.Mes,
                d.Anio,
                d.Horas,
                d.Monto,
                d.IVA,
                d.Total,
                d.Observaciones,
                d.PorcentajeCobro
            }), transaction);
    }

    public Task<int> MarcarAprobadaAsync(
        IDbConnection connection, IDbTransaction transaction, int solicitudId, int propuestaId)
    {
        return connection.ExecuteAsync(@"
            UPDATE propuestas SET Aprobada = 1
            WHERE PropuestaID = @PropuestaID AND SolicitudID = @SolicitudID AND Aprobada = 0;",
            new { SolicitudID = solicitudId, PropuestaID = propuestaId }, transaction);
    }

    private static async Task<PropuestaDetalle?> ObtenerDetalleCoreAsync(
        IDbConnection connection, IDbTransaction? transaction, int solicitudId, int propuestaId, bool bloquear)
    {
        var lockClause = bloquear ? " FOR UPDATE" : string.Empty;
        var sql = @"
            SELECT p.PropuestaID, p.SolicitudID, p.ProveedorID,
                   v.CedulaJuridica, v.Nombre AS NombreProveedor,
                   p.FechaPropuesta, p.Aprobada,
                   CASE WHEN EXISTS (
                       SELECT 1 FROM aprobaciones_propuesta ap
                       WHERE ap.SolicitudID = p.SolicitudID
                   ) OR EXISTS (
                       SELECT 1 FROM propuestas pa
                       WHERE pa.SolicitudID = p.SolicitudID AND pa.Aprobada = 1
                   ) THEN 1 ELSE 0 END AS SolicitudTieneAprobacion,
                   s.ConsecutivoOficio, s.Titulo AS TituloSolicitud
            FROM propuestas p
            INNER JOIN proveedores v ON v.ProveedorID = p.ProveedorID
            INNER JOIN solicitudes s ON s.SolicitudID = p.SolicitudID
            WHERE p.SolicitudID = @SolicitudID AND p.PropuestaID = @PropuestaID" + lockClause + ";";
        return await connection.QuerySingleOrDefaultAsync<PropuestaDetalle>(sql,
            new { SolicitudID = solicitudId, PropuestaID = propuestaId }, transaction);
    }
}

using System.Data;
using System.Text.Json;
using AdministracionSoluciones.Entities;
using AdministracionSoluciones.Entities.Propuestas;
using AdministracionSoluciones.Repository;
using AdministracionSoluciones.Services.Abstract;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace AdministracionSoluciones.Services;

/// <summary>Reglas de consulta y aprobación del módulo EXT7.</summary>
public sealed class PropuestaService : IPropuestaService
{
    private const string Modulo = "Propuestas";
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IPropuestaRepository _propuestas;
    private readonly IBitacoraService _bitacora;
    private readonly ILogger<PropuestaService> _logger;

    public PropuestaService(
        IDbConnectionFactory connectionFactory,
        IPropuestaRepository propuestas,
        IBitacoraService bitacora,
        ILogger<PropuestaService> logger)
    {
        _connectionFactory = connectionFactory;
        _propuestas = propuestas;
        _bitacora = bitacora;
        _logger = logger;
    }

    public Task<IReadOnlyList<PropuestaResumen>> ListarPorSolicitudAsync(int solicitudId)
    {
        if (solicitudId <= 0) return Task.FromResult<IReadOnlyList<PropuestaResumen>>([]);
        return _propuestas.ListarPorSolicitudAsync(solicitudId);
    }

    public Task<PropuestaDetalle?> ObtenerDetalleAsync(int solicitudId, int propuestaId)
    {
        if (solicitudId <= 0 || propuestaId <= 0) return Task.FromResult<PropuestaDetalle?>(null);
        return _propuestas.ObtenerDetalleAsync(solicitudId, propuestaId);
    }

    public Task<AprobacionPropuesta?> ObtenerAprobacionAsync(int solicitudId)
    {
        return solicitudId <= 0
            ? Task.FromResult<AprobacionPropuesta?>(null)
            : _propuestas.ObtenerAprobacionAsync(solicitudId);
    }

    public Task<IReadOnlyList<DesglosePropuestaHistorial>> ListarHistorialAsync(int solicitudId)
    {
        if (solicitudId <= 0)
            return Task.FromResult<IReadOnlyList<DesglosePropuestaHistorial>>([]);
        return _propuestas.ListarHistorialAsync(solicitudId);
    }

    public async Task<Resultado> AprobarAsync(int solicitudId, int propuestaId, string usuarioActual)
    {
        if (solicitudId <= 0 || propuestaId <= 0)
            return Resultado.Error("La solicitud o la propuesta indicada no es válida.");
        if (string.IsNullOrWhiteSpace(usuarioActual))
            return Resultado.Error("No se pudo identificar al usuario autenticado.");

        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        var committed = false;
        Exception? errorTecnico = null;

        try
        {
            // Todas las aprobaciones de una solicitud compiten por el mismo bloqueo.
            if (!await _propuestas.BloquearSolicitudAsync(connection, transaction, solicitudId))
                return Resultado.Error("La solicitud indicada no existe.");

            if (await _propuestas.TieneAprobacionAsync(connection, transaction, solicitudId))
                return Resultado.Error("Ya existe una propuesta aprobada para esta solicitud.");

            if (!await _propuestas.BloquearPropuestaAsync(
                connection, transaction, solicitudId, propuestaId))
                return Resultado.Error("La propuesta no existe o no pertenece a la solicitud seleccionada.");

            var propuesta = await _propuestas.ObtenerDetalleAsync(
                connection, transaction, solicitudId, propuestaId);
            if (propuesta == null)
                return Resultado.Error("La propuesta no existe o no pertenece a la solicitud seleccionada.");
            if (propuesta.Aprobada)
                return Resultado.Error("La propuesta ya fue aprobada.");
            if (propuesta.Lineas.Count == 0 || propuesta.Lineas.Any(linea => !LineaValida(linea)))
                return Resultado.Error("La propuesta no tiene líneas de desglose válidas.");

            var anteriores = await _propuestas.ListarDesglosesVigentesAsync(
                connection, transaction, solicitudId);
            var eliminados = await _propuestas.EliminarDesglosesVigentesAsync(
                connection, transaction, solicitudId);
            if (eliminados != anteriores.Count)
                throw new InvalidOperationException("No se pudieron bloquear y reemplazar todos los desgloses vigentes.");

            var insertados = await _propuestas.InsertarDesglosesPropuestosAsync(
                connection, transaction, solicitudId, propuestaId);
            if (insertados != propuesta.Lineas.Count)
                throw new InvalidOperationException("La cantidad de líneas trasladadas no coincide con la propuesta.");

            var nuevos = await _propuestas.ListarDesglosesVigentesTrasAprobacionAsync(
                connection, transaction, solicitudId);
            if (nuevos.Count != propuesta.Lineas.Count)
                throw new InvalidOperationException("No se pudo verificar el nuevo desglose formal.");

            var ahora = DateTime.Now;
            var datosAnteriores = JsonSerializer.Serialize(new
            {
                SolicitudID = solicitudId,
                Desgloses = anteriores
            });
            var datosNuevos = JsonSerializer.Serialize(new
            {
                SolicitudID = solicitudId,
                propuesta.PropuestaID,
                propuesta.ProveedorID,
                propuesta.CedulaJuridica,
                propuesta.NombreProveedor,
                propuesta.FechaPropuesta,
                Desgloses = nuevos
            });

            var aprobacionId = await _propuestas.RegistrarAprobacionAsync(connection, transaction,
                new AprobacionPropuesta
                {
                    SolicitudID = solicitudId,
                    PropuestaID = propuestaId,
                    ProveedorID = propuesta.ProveedorID,
                    CedulaJuridica = propuesta.CedulaJuridica,
                    NombreProveedor = propuesta.NombreProveedor,
                    Usuario = usuarioActual,
                    FechaAprobacion = ahora,
                    DatosAnterioresJson = datosAnteriores,
                    DatosNuevosJson = datosNuevos
                });

            var archivados = await _propuestas.ArchivarDesglosesAsync(
                connection, transaction, aprobacionId, anteriores);
            if (archivados != anteriores.Count)
                throw new InvalidOperationException("No se pudieron archivar todos los desgloses anteriores.");

            if (await _propuestas.MarcarAprobadaAsync(connection, transaction, solicitudId, propuestaId) != 1)
                throw new InvalidOperationException("La propuesta cambió de estado durante la aprobación.");

            await _bitacora.RegistrarAsync(usuarioActual, Modulo, "Aprobar propuesta", new
            {
                AprobacionID = aprobacionId,
                SolicitudID = solicitudId,
                propuesta.PropuestaID,
                propuesta.ProveedorID,
                propuesta.CedulaJuridica,
                propuesta.NombreProveedor,
                FechaAprobacion = ahora,
                DesglosesAnteriores = anteriores,
                DesglosesNuevos = nuevos
            }, connection, transaction);

            transaction.Commit();
            committed = true;
            return Resultado.Ok("La propuesta fue aprobada y el desglose de la solicitud fue reemplazado.");
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            _logger.LogInformation(ex, "Se rechazó una aprobación duplicada para la solicitud {SolicitudID}.", solicitudId);
            return Resultado.Error("Ya existe una propuesta aprobada para esta solicitud.");
        }
        catch (Exception ex)
        {
            errorTecnico = ex;
            _logger.LogError(ex,
                "Falló la aprobación de la propuesta {PropuestaID} para solicitud {SolicitudID}.",
                propuestaId, solicitudId);
            return Resultado.Error("No se pudo aprobar la propuesta. No se aplicaron cambios.");
        }
        finally
        {
            if (!committed)
            {
                try { transaction.Rollback(); }
                catch (Exception ex) { _logger.LogError(ex, "Falló la reversión de aprobación EXT7."); }
            }
            if (errorTecnico != null)
            {
                // El error se registra fuera de la transacción que acaba de revertirse.
                await _bitacora.RegistrarAsync(usuarioActual, Modulo, "Error al aprobar propuesta",
                    new { SolicitudID = solicitudId, PropuestaID = propuestaId, Error = errorTecnico.ToString() });
            }
        }
    }

    private static bool LineaValida(DesglosePropuesta linea)
    {
        return linea.Mes is >= 1 and <= 12
            && linea.Anio is >= 1 and <= 65535
            && linea.Horas is > 0 and <= 99999999.99m
            && linea.Monto is > 0 and <= 999999999999.99m
            && !string.IsNullOrWhiteSpace(linea.Observaciones)
            && linea.PorcentajeCobro is > 0 and <= 100;
    }
}


using System.Data.Common;
using AdministracionSoluciones.Entities;
using AdministracionSoluciones.Repository;
using AdministracionSoluciones.Services.Abstract;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace AdministracionSoluciones.Services;

public class SolicitudService :
    ISolicitudService,
    IProcesadorVencimientosSolicitudes
{
    private const string Modulo = "Solicitudes";
    private const int DiasHabilesVencimiento = 5;

    private static readonly string[] EstadosPermitidos =
    [
        "NUEVA",
        "RESPONDIDA",
        "INICIADA",
        "FINALIZADA",
        "VENCIDA"
    ];

    private readonly ISolicitudRepository _solicitudRepository;
    private readonly IBitacoraService _bitacoraService;
    private readonly ICorreoService _correoService;
    private readonly ICatalogoLookupService _catalogoService;
    private readonly ILogger<SolicitudService> _logger;

    public SolicitudService(
        ISolicitudRepository solicitudRepository,
        IBitacoraService bitacoraService,
        ICorreoService correoService,
        ICatalogoLookupService catalogoService,
        ILogger<SolicitudService> logger)
    {
        _solicitudRepository = solicitudRepository;
        _bitacoraService = bitacoraService;
        _correoService = correoService;
        _catalogoService = catalogoService;
        _logger = logger;
    }

    public async Task<(
        IEnumerable<Solicitud> Items,
        int Total,
        int Pagina)> ListarAsync(int pagina)
    {
        var resultado = await _solicitudRepository.ListarAsync(
            Math.Max(1, pagina));

        var items = resultado.Items.ToList();

        var estadoVencida =
            await _solicitudRepository.ObtenerIdEstadoAsync("VENCIDA");

        foreach (var solicitud in items)
        {
            solicitud.VenceHoy =
                solicitud.FechaRespuesta == null
                && (estadoVencida == null
                    || solicitud.EstadoSolicitudID != estadoVencida.Value)
                && ContarDiasHabiles(
                    solicitud.FechaIngreso.Date,
                    DateTime.Today) == DiasHabilesVencimiento;
        }

        return (items, resultado.Total, resultado.Pagina);
    }

    public Task<Solicitud?> ObtenerAsync(int id)
    {
        if (id <= 0)
        {
            return Task.FromResult<Solicitud?>(null);
        }

        return _solicitudRepository.ObtenerAsync(id);
    }

    public async Task<Resultado> CrearAsync(
        Solicitud solicitud,
        string usuarioActual)
    {
        var error = Validar(solicitud);

        if (error != null)
        {
            return error;
        }

        // Validar que el representante exista en el catálogo.
        if (solicitud.RepresentanteID.HasValue)
        {
            var representante = await _catalogoService.ObtenerAsync(
                TipoCatalogo.Representantes,
                solicitud.RepresentanteID.Value);

            if (representante == null)
            {
                return Resultado.Error(
                    "El representante seleccionado no existe.",
                    nameof(Solicitud.RepresentanteID));
            }
        }

        var estadoNueva =
            await _solicitudRepository.ObtenerIdEstadoAsync("NUEVA");

        if (estadoNueva == null)
        {
            return Resultado.Error(
                "No existe el estado inicial 'NUEVA' en los catálogos.");
        }

        solicitud.SolicitudID = 0;
        solicitud.ConsecutivoOficio =
            solicitud.ConsecutivoOficio.Trim();
        solicitud.Titulo = solicitud.Titulo.Trim();

        // El servidor establece el estado inicial.
        solicitud.EstadoSolicitudID = estadoNueva.Value;

        NormalizarCampos(solicitud);

        try
        {
            solicitud.SolicitudID =
                await _solicitudRepository.CrearAsync(solicitud);
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            return Resultado.Error(
                "Ya existe una solicitud con ese consecutivo de oficio.",
                nameof(Solicitud.ConsecutivoOficio));
        }
        catch (DbException)
        {
            return Resultado.Error(
                "No se pudo crear la solicitud. Verifique los datos ingresados y las relaciones con los catálogos.");
        }

        await _bitacoraService.RegistrarAsync(
            usuarioActual,
            Modulo,
            "Crear",
            DatosBitacora(solicitud));

        return Resultado.Ok(
            "La solicitud se creó correctamente.");
    }

    public async Task<Resultado> ActualizarAsync(
        Solicitud solicitud,
        string usuarioActual)
    {
        if (solicitud == null || solicitud.SolicitudID <= 0)
        {
            return Resultado.Error(
                "Los datos de la solicitud no son válidos.");
        }

        var existente = await _solicitudRepository.ObtenerAsync(
            solicitud.SolicitudID);

        if (existente == null)
        {
            return Resultado.Error(
                "La solicitud no existe.");
        }

        var error = Validar(solicitud);

        if (error != null)
        {
            return error;
        }

        // Validar que el representante exista en el catálogo.
        if (solicitud.RepresentanteID.HasValue)
        {
            var representante = await _catalogoService.ObtenerAsync(
                TipoCatalogo.Representantes,
                solicitud.RepresentanteID.Value);

            if (representante == null)
            {
                return Resultado.Error(
                    "El representante seleccionado no existe.",
                    nameof(Solicitud.RepresentanteID));
            }
        }

        // Validar que el estado exista y esté permitido.
        var estado = await _catalogoService.ObtenerAsync(
            TipoCatalogo.Estados,
            solicitud.EstadoSolicitudID);

        var estadoValido = estado != null
            && EstadosPermitidos.Contains(
                estado.Identificador,
                StringComparer.OrdinalIgnoreCase);

        if (!estadoValido)
        {
            return Resultado.Error(
                "Debe seleccionar un estado válido.",
                nameof(Solicitud.EstadoSolicitudID));
        }

        var estadoAnterior = existente.EstadoSolicitudID;

        solicitud.ConsecutivoOficio =
            solicitud.ConsecutivoOficio.Trim();
        solicitud.Titulo = solicitud.Titulo.Trim();

        NormalizarCampos(solicitud);

        try
        {
            var actualizado =
                await _solicitudRepository.ActualizarAsync(
                    solicitud,
                    estadoAnterior);

            if (!actualizado)
            {
                return Resultado.Error(
                    "No se pudo actualizar la solicitud.");
            }
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            return Resultado.Error(
                "Ya existe otra solicitud con ese consecutivo de oficio.",
                nameof(Solicitud.ConsecutivoOficio));
        }
        catch (DbException ex)
        {
            _logger.LogError(
                ex,
                "Falló la actualización de la solicitud {SolicitudID}.",
                solicitud.SolicitudID);
            return Resultado.Error(
                "No se pudo actualizar la solicitud. Verifique los datos ingresados y las relaciones con los catálogos.");
        }

        await _bitacoraService.RegistrarAsync(
            usuarioActual,
            Modulo,
            "Actualizar",
            new
            {
                Anterior = DatosBitacora(existente),
                Actual = DatosBitacora(solicitud)
            });

        if (estadoAnterior != solicitud.EstadoSolicitudID)
        {
            await _bitacoraService.RegistrarAsync(
                usuarioActual,
                Modulo,
                "Cambio de estado",
                new
                {
                    solicitud.SolicitudID,
                    EstadoAnteriorID = estadoAnterior,
                    EstadoNuevoID = solicitud.EstadoSolicitudID
                });

            var estadoAnteriorNombre =
                existente.IdentificadorEstado
                ?? estadoAnterior.ToString();

            var estadoNuevoNombre =
                await ObtenerIdentificadorEstadoAsync(
                    solicitud.EstadoSolicitudID);

            var correoEnviado =
                await _correoService.EnviarCambioEstadoAsync(
                    existente.EmailRepresentante ?? string.Empty,
                    solicitud.ConsecutivoOficio,
                    solicitud.Titulo,
                    estadoAnteriorNombre,
                    estadoNuevoNombre,
                    usuarioActual);

            if (!correoEnviado)
            {
                await _bitacoraService.RegistrarAsync(
                    usuarioActual,
                    Modulo,
                    "Notificación de cambio de estado fallida",
                    new { solicitud.SolicitudID });
                return Resultado.Ok(
                    "La solicitud se actualizó, pero no se pudo enviar el correo de notificación. Revise la configuración y los registros de la aplicación.");
            }
        }

        return Resultado.Ok(
            "La solicitud se actualizó correctamente.");
    }

    public async Task<Resultado> EliminarAsync(
        int id,
        string usuarioActual)
    {
        var solicitud = await _solicitudRepository.ObtenerAsync(id);

        if (solicitud == null)
        {
            return Resultado.Error(
                "La solicitud no existe.");
        }

        try
        {
            var eliminada =
                await _solicitudRepository.EliminarAsync(id);

            if (!eliminada)
            {
                return Resultado.Error(
                    "No se pudo eliminar la solicitud.");
            }
        }
        catch (MySqlException ex) when (ex.Number == 1451)
        {
            return Resultado.Error(
                "No se puede eliminar un registro con datos relacionados");
        }
        catch (DbException ex)
        {
            _logger.LogError(
                ex,
                "Falló la eliminación de la solicitud {SolicitudID}.",
                id);
            return Resultado.Error(
                "No se pudo eliminar la solicitud por un error de base de datos.");
        }

        await _bitacoraService.RegistrarAsync(
            usuarioActual,
            Modulo,
            "Eliminar",
            DatosBitacora(solicitud));

        return Resultado.Ok(
            "La solicitud se eliminó correctamente.");
    }

    public async Task ProcesarVencimientosAsync(
        CancellationToken cancellationToken = default)
    {
        var estadoVencida =
            await _solicitudRepository.ObtenerIdEstadoAsync("VENCIDA");

        if (estadoVencida == null)
        {
            _logger.LogError(
                "No existe el estado requerido VENCIDA; no se procesaron vencimientos.");
            return;
        }

        var pendientes =
            await _solicitudRepository
                .ListarPendientesDeVencimientoAsync();

        foreach (var solicitud in pendientes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (solicitud.FechaRespuesta.HasValue)
            {
                continue;
            }

            if (solicitud.EstadoSolicitudID == estadoVencida.Value)
            {
                continue;
            }

            if (!HanTranscurridoCincoDiasHabiles(
                solicitud.FechaIngreso.Date,
                DateTime.Today))
            {
                continue;
            }

            var estadoAnterior = solicitud.EstadoSolicitudID;

            // Solo cambia el estado; conserva los demás datos.
            bool actualizado;

            try
            {
                actualizado =
                    await _solicitudRepository.ActualizarEstadoAsync(
                        solicitud.SolicitudID,
                        solicitud.EstadoSolicitudID,
                        estadoVencida.Value);
            }
            catch (DbException ex)
            {
                _logger.LogError(
                    ex,
                    "Falló la actualización automática de vencimiento para la solicitud {SolicitudID}.",
                    solicitud.SolicitudID);
                continue;
            }

            if (!actualizado)
            {
                continue;
            }

            await _bitacoraService.RegistrarAsync(
                "Sistema",
                Modulo,
                "Vencimiento automático",
                new
                {
                    solicitud.SolicitudID,
                    solicitud.ConsecutivoOficio,
                    EstadoAnteriorID = estadoAnterior,
                    EstadoNuevoID = estadoVencida.Value,
                    solicitud.FechaIngreso
                });

            var correoEnviado =
                await _correoService.EnviarCambioEstadoAsync(
                    solicitud.EmailRepresentante ?? string.Empty,
                    solicitud.ConsecutivoOficio,
                    solicitud.Titulo,
                    solicitud.IdentificadorEstado
                        ?? estadoAnterior.ToString(),
                    "VENCIDA",
                    "Sistema");

            if (!correoEnviado)
            {
                await _bitacoraService.RegistrarAsync(
                    "Sistema",
                    Modulo,
                    "Notificación automática de vencimiento fallida",
                    new { solicitud.SolicitudID });
            }
        }
    }

    private async Task<string> ObtenerIdentificadorEstadoAsync(
        int estadoId)
    {
        foreach (var identificador in EstadosPermitidos)
        {
            var id = await _solicitudRepository.ObtenerIdEstadoAsync(
                identificador);

            if (id == estadoId)
            {
                return identificador;
            }
        }

        return estadoId.ToString();
    }

    private static bool HanTranscurridoCincoDiasHabiles(
        DateTime fechaIngreso,
        DateTime fechaActual)
    {
        return ContarDiasHabiles(fechaIngreso, fechaActual)
            > DiasHabilesVencimiento;
    }

    private static int ContarDiasHabiles(
        DateTime fechaIngreso,
        DateTime fechaActual)
    {
        var diasHabiles = 0;
        var fecha = fechaIngreso.Date;

        while (fecha < fechaActual.Date)
        {
            fecha = fecha.AddDays(1);

            if (fecha.DayOfWeek is not DayOfWeek.Saturday
                and not DayOfWeek.Sunday)
            {
                diasHabiles++;
            }
        }

        return diasHabiles;
    }

    private static Resultado? Validar(Solicitud solicitud)
    {
        if (solicitud == null)
        {
            return Resultado.Error(
                "Los datos de la solicitud son obligatorios.");
        }

        if (string.IsNullOrWhiteSpace(solicitud.ConsecutivoOficio))
        {
            return Resultado.Error(
                "El consecutivo de oficio es obligatorio.",
                nameof(Solicitud.ConsecutivoOficio));
        }

        if (solicitud.ConsecutivoOficio.Trim().Length > 100)
        {
            return Resultado.Error(
                "El consecutivo no puede superar los 100 caracteres.",
                nameof(Solicitud.ConsecutivoOficio));
        }

        if (string.IsNullOrWhiteSpace(solicitud.Titulo))
        {
            return Resultado.Error(
                "El título es obligatorio.",
                nameof(Solicitud.Titulo));
        }

        if (solicitud.Titulo.Trim().Length > 200)
        {
            return Resultado.Error(
                "El título no puede superar los 200 caracteres.",
                nameof(Solicitud.Titulo));
        }

        if (solicitud.FechaIngreso == default)
        {
            return Resultado.Error(
                "La fecha de ingreso es obligatoria.",
                nameof(Solicitud.FechaIngreso));
        }

        if (solicitud.RepresentanteID is <= 0)
        {
            return Resultado.Error(
                "El representante seleccionado no es válido.",
                nameof(Solicitud.RepresentanteID));
        }

        if (solicitud.FechaRespuesta.HasValue &&
            solicitud.FechaRespuesta.Value.Date <
            solicitud.FechaIngreso.Date)
        {
            return Resultado.Error(
                "La fecha de respuesta no puede ser anterior a la fecha de ingreso.",
                nameof(Solicitud.FechaRespuesta));
        }

        if (solicitud.FechaInicio.HasValue &&
            solicitud.FechaInicio.Value.Date <
            solicitud.FechaIngreso.Date)
        {
            return Resultado.Error(
                "La fecha de inicio no puede ser anterior a la fecha de ingreso.",
                nameof(Solicitud.FechaInicio));
        }

        return null;
    }

    private static void NormalizarCampos(Solicitud solicitud)
    {
        solicitud.DocumentoRespuesta =
            LimpiarOpcional(solicitud.DocumentoRespuesta);

        solicitud.DocumentoInicio =
            LimpiarOpcional(solicitud.DocumentoInicio);

        solicitud.Descripcion =
            LimpiarOpcional(solicitud.Descripcion);

        solicitud.Observaciones =
            LimpiarOpcional(solicitud.Observaciones);
    }

    private static string? LimpiarOpcional(string? valor)
    {
        return string.IsNullOrWhiteSpace(valor)
            ? null
            : valor.Trim();
    }

    private static object DatosBitacora(Solicitud solicitud)
    {
        return new
        {
            solicitud.SolicitudID,
            solicitud.ConsecutivoOficio,
            solicitud.Titulo,
            solicitud.RepresentanteID,
            solicitud.EstadoSolicitudID,
            solicitud.FechaIngreso,
            solicitud.FechaRespuesta,
            solicitud.FechaInicio
        };
    }
}

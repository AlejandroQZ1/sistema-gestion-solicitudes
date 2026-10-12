using System.Text;
using AdministracionSoluciones.Entities;
using AdministracionSoluciones.Repository;
using AdministracionSoluciones.Services.Abstract;
using Microsoft.Extensions.Logging;

namespace AdministracionSoluciones.Services;

// Coordina las validaciones y auditorias del mantenimiento y los reportes de tareas.
public class TareaService : ITareaService
{
    private readonly TareaRepository repositorio;
    private readonly IBitacoraService bitacora;
    private readonly ILogger<TareaService> logger;

    public TareaService(TareaRepository repositorio, IBitacoraService bitacora, ILogger<TareaService> logger)
    {
        this.repositorio = repositorio;
        this.bitacora = bitacora;
        this.logger = logger;
    }

    // Los selectores excluyen las contraseñas y otros datos ajenos a tareas.
    public Task<IReadOnlyList<UsuarioOpcion>> UsuariosAsync() => repositorio.UsuariosAsync();
    public Task<IReadOnlyList<SolicitudOpcion>> SolicitudesAsync() => repositorio.SolicitudesAsync();
    public Task<SolicitudOpcion?> SolicitudAsync(int id) => repositorio.SolicitudAsync(id);

    // Se registra la consulta que muestra la pagina de tareas.
    public async Task<(IReadOnlyList<TareaFila> Filas, long Cantidad)> ListarAsync(int solicitudId, int pagina, string actor)
    {
        var resultado = await repositorio.ListarAsync(solicitudId, Math.Max(1, pagina));
        await ConsultaAsync(actor, "tareas");
        return resultado;
    }

    // La consulta individual verifica tambien la solicitud propietaria de la tarea.
    public async Task<TareaFila?> ObtenerAsync(int id, int solicitudId, string actor)
    {
        var tarea = await repositorio.ObtenerAsync(id, solicitudId);
        await ConsultaAsync(actor, "tarea");
        return tarea;
    }

    // Los limites corresponden a los tipos SQL y a las relaciones del esquema.
    public async Task<string?> ValidarAsync(TareaDatos datos)
    {
        var usuarios = await repositorio.UsuariosAsync();
        if (!datos.UsuarioId.HasValue || !usuarios.Any(u => u.Id == datos.UsuarioId))
        {
            return "Seleccione un usuario existente.";
        }
        if (datos.Fecha.HasValue && datos.Fecha.Value.Year < 1000)
        {
            return "La fecha debe pertenecer al año 1000 o posterior.";
        }
        if (datos.Horas.HasValue && (datos.Horas < -99999999.99m || datos.Horas > 99999999.99m || decimal.Round(datos.Horas.Value, 2) != datos.Horas))
        {
            return "Las horas admiten hasta ocho enteros y dos decimales.";
        }
        if (datos.Descripcion != null && Encoding.UTF8.GetByteCount(datos.Descripcion) > 65535)
        {
            return "La descripción supera el tamaño admitido.";
        }
        return null;
    }

    // Las operaciones del repositorio confirman los datos y la bitacora juntos.
    public async Task CrearAsync(int solicitudId, TareaDatos datos, string actor)
    {
        await ComprobarAsync(datos);
        await repositorio.CrearAsync(solicitudId, datos, actor);
    }

    public async Task<bool> EditarAsync(int id, int solicitudId, TareaDatos datos, string actor)
    {
        await ComprobarAsync(datos);
        return await repositorio.EditarAsync(id, solicitudId, datos, actor);
    }

    public Task<bool> EliminarAsync(int id, int solicitudId, string actor)
    {
        return repositorio.EliminarAsync(id, solicitudId, actor);
    }

    // La validacion tambien se aplica si otro modulo invoca el servicio directamente.
    private async Task ComprobarAsync(TareaDatos datos)
    {
        var error = await ValidarAsync(datos);
        if (error != null)
        {
            throw new ArgumentException(error);
        }
    }

    // El reporte incluye todas las filas del periodo; su total no depende de paginacion.
    public async Task<IReadOnlyList<TareaFila>> ReporteAsync(bool porUsuario, int seleccionId, int mes, int anio, string actor)
    {
        if (mes < 1 || mes > 12 || anio < 1000 || anio > 9999 || seleccionId <= 0)
        {
            throw new ArgumentException("Los filtros del reporte no son válidos.");
        }
        var filas = await repositorio.ReporteAsync(porUsuario, seleccionId, mes, anio);
        await ConsultaAsync(actor, porUsuario ? "reporte por usuario" : "reporte por solicitud");
        return filas;
    }

    public Task ConsultaAsync(string actor, string elemento)
    {
        return bitacora.RegistrarAsync(actor, "Tareas", $"El usuario consulta {elemento}");
    }

    // El detalle tecnico queda en la bitacora y el log, sin mostrarse en la pantalla.
    public async Task RegistrarErrorAsync(string actor, Exception error, string referencia)
    {
        logger.LogError(error, "Error de tareas. Referencia {Referencia}", referencia);
        try
        {
            await bitacora.RegistrarAsync(actor, "Tareas", "Error técnico", new { Tipo = error.GetType().Name, error.Message, Referencia = referencia });
        }
        catch (Exception falloBitacora)
        {
            logger.LogError(falloBitacora, "No fue posible escribir la bitacora. Referencia {Referencia}", referencia);
        }
    }
}

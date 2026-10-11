using AdministracionSoluciones.Entities;

namespace AdministracionSoluciones.Services.Abstract;

// Contrato de TAR1, TAR2 y TAR3 utilizado por las paginas Razor.
public interface ITareaService
{
    Task<IReadOnlyList<UsuarioOpcion>> UsuariosAsync();
    Task<IReadOnlyList<SolicitudOpcion>> SolicitudesAsync();
    Task<SolicitudOpcion?> SolicitudAsync(int id);
    Task<(IReadOnlyList<TareaFila> Filas, long Cantidad)> ListarAsync(int solicitudId, int pagina, string actor);
    Task<TareaFila?> ObtenerAsync(int id, int solicitudId, string actor);
    Task<string?> ValidarAsync(TareaDatos datos);
    Task CrearAsync(int solicitudId, TareaDatos datos, string actor);
    Task<bool> EditarAsync(int id, int solicitudId, TareaDatos datos, string actor);
    Task<bool> EliminarAsync(int id, int solicitudId, string actor);
    Task<IReadOnlyList<TareaFila>> ReporteAsync(bool porUsuario, int seleccionId, int mes, int anio, string actor);
    Task ConsultaAsync(string actor, string elemento);
    Task RegistrarErrorAsync(string actor, Exception error, string referencia);
}


namespace AdministracionSoluciones.Services.Abstract;

public interface ICorreoService
{
    Task<bool> EnviarCambioEstadoAsync(
        string destinatario,
        string consecutivoOficio,
        string tituloSolicitud,
        string estadoAnterior,
        string estadoNuevo,
        string usuarioActual);
}


using AdministracionSoluciones.Entities;

namespace AdministracionSoluciones.Services.Abstract
{
    public interface ISolicitudService
    {
        Task<(IEnumerable<Solicitud> Items, int Total, int Pagina)>
            ListarAsync(int pagina);

        Task<Solicitud?> ObtenerAsync(int id);

        Task<Resultado> CrearAsync(
            Solicitud solicitud,
            string usuarioActual);

        Task<Resultado> ActualizarAsync(
            Solicitud solicitud,
            string usuarioActual);

        Task<Resultado> EliminarAsync(
            int id,
            string usuarioActual);

    }
}

using AdministracionSoluciones.Entities;

namespace AdministracionSoluciones.Repository
{
    public interface ISolicitudRepository
    {
        Task<(
        IEnumerable<Solicitud> Items,
        int Total,
        int Pagina)> ListarAsync(int pagina);

        Task<Solicitud?> ObtenerAsync(int id);

        Task<int> CrearAsync(Solicitud solicitud);

        Task<bool> ActualizarAsync(Solicitud solicitud, int estadoAnteriorId);

        Task<bool> ActualizarPublicacionAsync(
            int solicitudId,
            bool publicadoAnterior,
            DateTime? fechaAnterior,
            bool publicadoNuevo,
            DateTime? fechaNueva);

        Task<IEnumerable<Solicitud>> ListarPublicadasVigentesAsync(
            DateTime fechaActual);

        Task<bool> ActualizarEstadoAsync(
            int solicitudId,
            int estadoAnteriorId,
            int estadoNuevoId);

        Task<bool> EliminarAsync(int id);

        Task<int?> ObtenerIdEstadoAsync(
            string identificador);

        Task<IEnumerable<Solicitud>>
            ListarPendientesDeVencimientoAsync();
    }

}

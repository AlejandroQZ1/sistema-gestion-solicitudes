namespace AdministracionSoluciones.Services.Abstract;

public interface IProcesadorVencimientosSolicitudes
{
    Task ProcesarVencimientosAsync(CancellationToken cancellationToken = default);
}

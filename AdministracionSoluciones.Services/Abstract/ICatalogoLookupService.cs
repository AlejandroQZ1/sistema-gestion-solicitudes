using AdministracionSoluciones.Entities;

namespace AdministracionSoluciones.Services.Abstract;

public interface ICatalogoLookupService
{
    Task<CatalogoItem?> ObtenerAsync(TipoCatalogo tipo, int id);
}

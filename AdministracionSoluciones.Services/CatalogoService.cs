using System.ComponentModel.DataAnnotations;
using AdministracionSoluciones.Entities;
using AdministracionSoluciones.Repository;
using AdministracionSoluciones.Services.Abstract;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace AdministracionSoluciones.Services;

public class CatalogoService(CatalogoRepository repository, IBitacoraService bitacora, ILogger<CatalogoService> logger)
{
    public Task<CatalogoItem?> ObtenerAsync(TipoCatalogo tipo, int id) => repository.ObtenerAsync(tipo, id);

    public async Task<(List<CatalogoItem> Items, int Total, int Pagina)> ListarAsync(TipoCatalogo tipo, int pagina, string usuario)
    {
        try
        {
            var datos = await repository.ListarAsync(tipo, pagina);
            await bitacora.RegistrarAsync(usuario, tipo.ToString(), $"El usuario consulta {tipo}");
            return datos;
        }
        catch (Exception ex) { await RegistrarErrorAsync(tipo, usuario, ex); throw; }
    }

    public async Task<Resultado> GuardarAsync(TipoCatalogo tipo, CatalogoItem item, string usuario, bool eliminar = false)
    {
        item.Nombre = item.Nombre.Trim();
        item.Email = item.Email.Trim();
        item.Identificador = item.Identificador.Trim();
        if (!eliminar)
        {
            if (string.IsNullOrWhiteSpace(item.Nombre) || item.Nombre.Length > 150)
                return Resultado.Error("Ingrese un nombre de hasta 150 caracteres.");
            if (tipo == TipoCatalogo.Representantes && (item.Email.Length > 150 || !new EmailAddressAttribute().IsValid(item.Email)))
                return Resultado.Error("Ingrese un correo electrónico válido de hasta 150 caracteres.");
            if (tipo == TipoCatalogo.Estados && (string.IsNullOrWhiteSpace(item.Identificador) || item.Identificador.Length > 50))
                return Resultado.Error("Ingrese un identificador de hasta 50 caracteres.");
        }
        try
        {
            if (!await repository.GuardarAsync(tipo, item, usuario, eliminar)) return Resultado.Error("El registro ya no existe.");
            return Resultado.Ok(eliminar ? "Registro eliminado correctamente." : "Registro guardado correctamente.");
        }
        catch (MySqlException ex) when (ex.Number == 1451)
        {
            return Resultado.Error("No se puede eliminar un registro con datos relacionados.");
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            return Resultado.Error("Ya existe un estado con ese identificador.");
        }
        catch (Exception ex)
        {
            await RegistrarErrorAsync(tipo, usuario, ex);
            return Resultado.Error("No se pudo guardar el cambio. Intente nuevamente.");
        }
    }

    public async Task RegistrarErrorAsync(TipoCatalogo tipo, string usuario, Exception ex)
    {
        logger.LogError(ex, "Error en el catálogo {Catalogo}", tipo);
        try { await bitacora.RegistrarAsync(usuario, tipo.ToString(), "Error técnico", new { Tipo = ex.GetType().Name, ex.Message }); }
        catch (Exception auditError) { logger.LogError(auditError, "No se pudo registrar el error en la bitácora"); }
    }
}

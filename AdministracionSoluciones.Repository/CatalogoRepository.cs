using System.Data;
using System.Text.Json;
using AdministracionSoluciones.Entities;
using Dapper;

namespace AdministracionSoluciones.Repository;

public class CatalogoRepository(IDbConnectionFactory factory)
{
    private static (string Tabla, string Id, string Campo) Config(TipoCatalogo tipo) => tipo switch
    {
        TipoCatalogo.Representantes => ("representantes", "RepresentanteID", "Email"),
        TipoCatalogo.Estados => ("estados_solicitud", "EstadoSolicitudID", "Identificador"),
        _ => throw new ArgumentOutOfRangeException(nameof(tipo))
    };

    public async Task<(List<CatalogoItem> Items, int Total, int Pagina)> ListarAsync(TipoCatalogo tipo, int pagina)
    {
        var (tabla, id, campo) = Config(tipo);
        using var db = factory.CreateConnection();
        var total = await db.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {tabla}");
        pagina = Math.Clamp(pagina, 1, Math.Max(1, (total + 9) / 10));
        var items = await db.QueryAsync<CatalogoItem>($"SELECT {id} AS Id, Nombre, {campo} FROM {tabla} ORDER BY Nombre, {id} LIMIT 10 OFFSET @Offset", new { Offset = (pagina - 1) * 10 });
        return (items.ToList(), total, pagina);
    }

    public async Task<CatalogoItem?> ObtenerAsync(TipoCatalogo tipo, int clave)
    {
        var (tabla, id, campo) = Config(tipo);
        using var db = factory.CreateConnection();
        return await db.QuerySingleOrDefaultAsync<CatalogoItem>($"SELECT {id} AS Id, Nombre, {campo} FROM {tabla} WHERE {id} = @clave", new { clave });
    }

    // La modificación y su bitácora se confirman juntas; si falla una, se revierten ambas.
    public async Task<bool> GuardarAsync(TipoCatalogo tipo, CatalogoItem item, string usuario, bool eliminar = false)
    {
        var (tabla, id, campo) = Config(tipo);
        using var db = factory.CreateConnection();
        db.Open();
        using var tx = db.BeginTransaction();
        var anterior = item.Id == 0 ? null : await db.QuerySingleOrDefaultAsync<CatalogoItem>(
            $"SELECT {id} AS Id, Nombre, {campo} FROM {tabla} WHERE {id} = @Id FOR UPDATE", item, tx);
        if (item.Id != 0 && anterior == null) return false;
        var accion = eliminar ? "Eliminar" : item.Id == 0 ? "Crear" : "Actualizar";
        if (eliminar)
            await db.ExecuteAsync($"DELETE FROM {tabla} WHERE {id} = @Id", item, tx);
        else if (item.Id == 0)
            item.Id = await db.ExecuteScalarAsync<int>($"INSERT INTO {tabla} (Nombre, {campo}) VALUES (@Nombre, @{campo}); SELECT LAST_INSERT_ID();", item, tx);
        else
            await db.ExecuteAsync($"UPDATE {tabla} SET Nombre = @Nombre, {campo} = @{campo} WHERE {id} = @Id", item, tx);
        object Datos(CatalogoItem x) => tipo == TipoCatalogo.Representantes
            ? new { x.Id, x.Nombre, x.Email } : (object)new { x.Id, x.Identificador, x.Nombre };
        object detalle = eliminar ? Datos(anterior!) : anterior == null ? Datos(item) : new { Anterior = Datos(anterior), Actual = Datos(item) };
        await db.ExecuteAsync("INSERT INTO bitacora (Fecha, Usuario, Modulo, Accion, Detalle) VALUES (@Fecha, @Usuario, @Modulo, @Accion, @Detalle)",
            new { Fecha = DateTime.Now, Usuario = usuario, Modulo = tipo.ToString(), Accion = accion, Detalle = JsonSerializer.Serialize(detalle) }, tx);
        tx.Commit();
        return true;
    }
}

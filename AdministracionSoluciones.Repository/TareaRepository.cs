using Dapper;
using MySqlConnector;
using AdministracionSoluciones.Entities;
using System.Data;
using System.Text.Json;

namespace AdministracionSoluciones.Repository;

// Consultas de tareas y operaciones atomicas con su registro de auditoria.
public class TareaRepository
{
    private readonly IDbConnectionFactory conexiones;

    // La factoria proporciona las conexiones MySQL configuradas para siges.
    public TareaRepository(IDbConnectionFactory conexiones)
    {
        this.conexiones = conexiones;
    }

    // Los alias relacionan los nombres SQL con las propiedades del modelo TareaFila.
    private const string Columnas = """
        SELECT t.TareaID AS Id, t.SolicitudID AS SolicitudId,
               t.DescripcionTarea AS Descripcion, t.Fecha AS Fecha,
               t.UsuarioID AS UsuarioId, t.HorasInvertidas AS Horas,
               u.NombreUsuario AS NombreUsuario, u.NombreCompleto AS NombreCompleto,
               s.ConsecutivoOficio AS Consecutivo, s.Titulo AS Titulo
        FROM tareas t
        JOIN usuarios u ON u.UsuarioID = t.UsuarioID
        JOIN solicitudes s ON s.SolicitudID = t.SolicitudID
        """;

    // Obtiene los usuarios disponibles para asignar tareas, sin consultar contraseñas.
    public async Task<IReadOnlyList<UsuarioOpcion>> UsuariosAsync()
    {
        using var conexion = conexiones.CreateConnection();
        return (await conexion.QueryAsync<UsuarioOpcion>("""
            SELECT UsuarioID AS Id, NombreUsuario AS NombreUsuario, NombreCompleto AS NombreCompleto
            FROM usuarios ORDER BY NombreCompleto, UsuarioID
            """)).AsList();
    }

    // Obtiene las solicitudes utilizadas por el filtro del reporte mensual.
    public async Task<IReadOnlyList<SolicitudOpcion>> SolicitudesAsync()
    {
        using var conexion = conexiones.CreateConnection();
        return (await conexion.QueryAsync<SolicitudOpcion>("""
            SELECT SolicitudID AS Id, ConsecutivoOficio AS Consecutivo, Titulo AS Titulo
            FROM solicitudes ORDER BY ConsecutivoOficio, SolicitudID
            """)).AsList();
    }

    // Busca una solicitud por su identificador.
    public async Task<SolicitudOpcion?> SolicitudAsync(int id)
    {
        using var conexion = conexiones.CreateConnection();
        return await conexion.QuerySingleOrDefaultAsync<SolicitudOpcion>("""
            SELECT SolicitudID AS Id, ConsecutivoOficio AS Consecutivo, Titulo AS Titulo
            FROM solicitudes WHERE SolicitudID = @Id
            """, new { Id = id });
    }

    // Consulta una página de diez tareas y el total de registros de la solicitud.
    public async Task<(IReadOnlyList<TareaFila> Filas, long Cantidad)> ListarAsync(int solicitudId, int pagina)
    {
        using var conexion = conexiones.CreateConnection();
        var cantidad = await conexion.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM tareas WHERE SolicitudID = @SolicitudId", new { SolicitudId = solicitudId });
        // El registro recién creado aparece en la primera página, aunque tenga una Fecha anterior.
        var filas = await conexion.QueryAsync<TareaFila>(Columnas + " WHERE t.SolicitudID = @SolicitudId ORDER BY t.TareaID DESC LIMIT 10 OFFSET @Desde",
            new { SolicitudId = solicitudId, Desde = ((long)pagina - 1) * 10 });
        return (filas.AsList(), cantidad);
    }

    // Busca la tarea por su identificador y verifica a qué solicitud pertenece.
    public async Task<TareaFila?> ObtenerAsync(int id, int solicitudId)
    {
        using var conexion = conexiones.CreateConnection();
        return await conexion.QuerySingleOrDefaultAsync<TareaFila>(Columnas + " WHERE t.TareaID = @Id AND t.SolicitudID = @SolicitudId", new { Id = id, SolicitudId = solicitudId });
    }

    // Inserta la tarea y su bitácora en una misma transacción.
    public async Task<int> CrearAsync(int solicitudId, TareaDatos modelo, string actor)
    {
        using var conexion = conexiones.CreateConnection();
        conexion.Open();
        using var transaccion = conexion.BeginTransaction();
        var id = await conexion.ExecuteScalarAsync<int>("""
            INSERT INTO tareas(SolicitudID, DescripcionTarea, Fecha, UsuarioID, HorasInvertidas)
            VALUES (@SolicitudId, @Descripcion, @Fecha, @UsuarioId, @Horas);
            SELECT LAST_INSERT_ID();
            """, new { SolicitudId = solicitudId, modelo.Descripcion, modelo.Fecha, modelo.UsuarioId, modelo.Horas }, transaccion);
        await GuardarBitacoraAsync(conexion, transaccion, actor, "Crear tarea", new { Id = id, SolicitudId = solicitudId, modelo.Descripcion, modelo.Fecha, modelo.UsuarioId, modelo.Horas });
        transaccion.Commit();
        return id;
    }

    // Actualiza la tarea y registra los datos anteriores y actuales.
    public async Task<bool> EditarAsync(int id, int solicitudId, TareaDatos modelo, string actor)
    {
        using var conexion = conexiones.CreateConnection();
        conexion.Open();
        using var transaccion = conexion.BeginTransaction();
        var anterior = await BloquearAsync(conexion, transaccion, id, solicitudId);
        if (anterior is null)
        {
            return false;
        }
        await conexion.ExecuteAsync("""
            UPDATE tareas SET DescripcionTarea=@Descripcion, Fecha=@Fecha, UsuarioID=@UsuarioId, HorasInvertidas=@Horas
            WHERE TareaID=@Id AND SolicitudID=@SolicitudId
            """, new { Id = id, SolicitudId = solicitudId, modelo.Descripcion, modelo.Fecha, modelo.UsuarioId, modelo.Horas }, transaccion);
        await GuardarBitacoraAsync(conexion, transaccion, actor, "Actualizar tarea", new
        {
            Anterior = anterior.DatosBitacora(),
            Actual = new { Id = id, SolicitudId = solicitudId, modelo.Descripcion, modelo.Fecha, modelo.UsuarioId, modelo.Horas }
        });
        transaccion.Commit();
        return true;
    }

    // Elimina la tarea y conserva sus datos en la bitácora.
    public async Task<bool> EliminarAsync(int id, int solicitudId, string actor)
    {
        using var conexion = conexiones.CreateConnection();
        conexion.Open();
        using var transaccion = conexion.BeginTransaction();
        var anterior = await BloquearAsync(conexion, transaccion, id, solicitudId);
        if (anterior is null)
        {
            return false;
        }
        // La FK de cualquier registro dependiente impide el borrado (error 1451).
        await conexion.ExecuteAsync("DELETE FROM tareas WHERE TareaID=@Id AND SolicitudID=@SolicitudId", new { Id = id, SolicitudId = solicitudId }, transaccion);
        await GuardarBitacoraAsync(conexion, transaccion, actor, "Eliminar tarea", anterior.DatosBitacora());
        transaccion.Commit();
        return true;
    }

    // Bloquea la fila durante la transacción para que la bitácora conserve su estado previo real.
    private static Task<TareaFila?> BloquearAsync(IDbConnection conexion, IDbTransaction transaccion, int id, int solicitudId)
    {
        return conexion.QuerySingleOrDefaultAsync<TareaFila>("""
            SELECT TareaID AS Id, SolicitudID AS SolicitudId, DescripcionTarea AS Descripcion,
                   Fecha AS Fecha, UsuarioID AS UsuarioId, HorasInvertidas AS Horas
            FROM tareas WHERE TareaID=@Id AND SolicitudID=@SolicitudId FOR UPDATE
            """, new { Id = id, SolicitudId = solicitudId }, transaccion);
    }

    // Consulta todas las tareas del usuario o solicitud dentro del periodo mensual.
    public async Task<IReadOnlyList<TareaFila>> ReporteAsync(bool porUsuario, int seleccionId, int mes, int anio)
    {
        var inicio = new DateTime(anio, mes, 1);
        // El último mes admitido por DateTime no tiene un mes siguiente representable.
        DateTime? fin = null;
        if (anio != 9999 || mes != 12)
        {
            fin = inicio.AddMonths(1);
        }
        // La columna se elige entre dos constantes internas, nunca desde texto del usuario.
        var campo = porUsuario ? "t.UsuarioID" : "t.SolicitudID";
        using var conexion = conexiones.CreateConnection();
        return (await conexion.QueryAsync<TareaFila>(Columnas + $" WHERE {campo}=@SeleccionId AND t.Fecha>=@Inicio AND (@Fin IS NULL OR t.Fecha<@Fin) ORDER BY t.Fecha, t.TareaID",
            new { SeleccionId = seleccionId, Inicio = inicio, Fin = fin })).AsList();
    }
    // La bitacora participa en la misma transaccion que la tarea.
    private static Task GuardarBitacoraAsync(IDbConnection conexion, IDbTransaction transaccion,
        string actor, string accion, object datos)
    {
        return conexion.ExecuteAsync(
            "INSERT INTO bitacora (Fecha, Usuario, Modulo, Accion, Detalle) VALUES (@Fecha, @Usuario, 'Tareas', @Accion, @Detalle)",
            new { Fecha = DateTime.Now, Usuario = actor, Accion = accion, Detalle = JsonSerializer.Serialize(datos) }, transaccion);
    }
}

using System.Data;
using System.Data.Common;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using AdministracionSoluciones.Entities;
using AdministracionSoluciones.Entities.Propuestas;
using AdministracionSoluciones.Repository;
using AdministracionSoluciones.Services;
using AdministracionSoluciones.Services.Abstract;
using Microsoft.Extensions.Logging.Abstractions;

var pruebas = new (string Nombre, Func<Task> Ejecutar)[]
{
    ("aprobación reemplaza vigentes y conserva historial", AprobacionCorrecta),
    ("propuesta inexistente no cambia datos", PropuestaInexistente),
    ("propuesta de otra solicitud se rechaza", PropuestaDeOtraSolicitud),
    ("propuesta sin líneas se rechaza", PropuestaSinLineas),
    ("segunda aprobación se rechaza", AprobacionDuplicada),
    ("dos aprobaciones simultáneas dejan una sola aprobada", AprobacionesSimultaneas),
    ("error transaccional revierte eliminación y no registra bitácora", ErrorTransaccional),
    ("error de bitácora revierte toda la operación", ErrorBitacora),
    ("actor vacío se rechaza", ActorNoAutenticado)
};

var fallas = 0;
foreach (var prueba in pruebas)
{
    try
    {
        await prueba.Ejecutar();
        Console.WriteLine($"PASS {prueba.Nombre}");
    }
    catch (Exception ex)
    {
        fallas++;
        Console.WriteLine($"FAIL {prueba.Nombre}: {ex.Message}");
    }
}

Console.WriteLine($"Resultado: {pruebas.Length - fallas}/{pruebas.Length} pruebas pasaron.");
return fallas == 0 ? 0 : 1;

static async Task AprobacionCorrecta()
{
    var (service, repo, log) = NuevoCaso();
    var result = await service.AprobarAsync(1, 10, "ana");
    Assert(result.Exito, result.Mensaje);
    Assert(repo.Vigentes(1).Count == 1, "El conjunto formal debe ser reemplazado por la línea aprobada.");
    Assert(repo.Vigentes(1)[0].DesglosePropuestaID == 100, "Debe quedar el vínculo a la línea fuente.");
    Assert(repo.Historial(1).Count == 1 && repo.Historial(1)[0].DesgloseID == 50,
        "El desglose anterior debe preservarse con su ID y datos.");
    Assert(repo.Aprobacion(1)?.Usuario == "ana", "Debe persistirse el usuario aprobador.");
    Assert(repo.Aprobacion(1)?.FechaAprobacion != default, "Debe persistirse la fecha.");
    Assert(repo.Aprobacion(1)?.DatosAnterioresJson.Contains("desglose anterior") == true,
        "El snapshot anterior debe quedar auditado.");
    Assert(repo.Aprobacion(1)?.DatosNuevosJson.Contains("detalle propuesto") == true,
        "El snapshot nuevo debe quedar auditado.");
    Assert(log.Actor == "ana" && log.Modulo == "Propuestas" && log.Accion == "Aprobar propuesta",
        "La bitácora debe usar el usuario de sesión y la acción EXT7.");
}

static async Task PropuestaInexistente()
{
    var (service, repo, _) = NuevoCaso();
    var result = await service.AprobarAsync(1, 999, "ana");
    Assert(!result.Exito, "La propuesta inexistente debía fallar.");
    Assert(repo.Vigentes(1)[0].DesgloseID == 50 && repo.Aprobacion(1) == null,
        "Una propuesta inexistente no debe cambiar los desgloses.");
}

static async Task PropuestaDeOtraSolicitud()
{
    var (service, repo, _) = NuevoCaso();
    var result = await service.AprobarAsync(2, 10, "ana");
    Assert(!result.Exito, "No se debe aceptar una propuesta asociada a otra solicitud.");
    Assert(repo.Vigentes(1)[0].DesgloseID == 50 && repo.Aprobacion(1) == null,
        "La solicitud propietaria no debe cambiar.");
}

static async Task PropuestaSinLineas()
{
    var (service, repo, _) = NuevoCaso();
    repo.AgregarPropuesta(11, 1, []);
    var result = await service.AprobarAsync(1, 11, "ana");
    Assert(!result.Exito, "No se debe aprobar una propuesta vacía.");
    Assert(repo.Vigentes(1)[0].DesgloseID == 50 && repo.Aprobacion(1) == null,
        "La propuesta vacía no debe reemplazar el desglose.");
}

static async Task AprobacionDuplicada()
{
    var (service, repo, _) = NuevoCaso();
    Assert((await service.AprobarAsync(1, 10, "ana")).Exito, "La primera aprobación debe funcionar.");
    var result = await service.AprobarAsync(1, 10, "ana");
    Assert(!result.Exito, "La segunda aprobación debe rechazarse.");
    Assert(repo.Historial(1).Count == 1 && repo.Vigentes(1).Count == 1,
        "El segundo intento no debe duplicar ni rearchivar filas.");
}

static async Task AprobacionesSimultaneas()
{
    var (service, repo, _) = NuevoCaso();
    repo.AgregarPropuesta(11, 1, [Linea(101, 11, "segunda propuesta")]);
    var resultados = await Task.WhenAll(
        service.AprobarAsync(1, 10, "ana"),
        service.AprobarAsync(1, 11, "bea"));
    Assert(resultados.Count(r => r.Exito) == 1, "Solo una aprobación concurrente puede ganar.");
    Assert(repo.Aprobacion(1) is not null, "Debe persistir una aprobación.");
    Assert(repo.Historial(1).Count == 1, "El conjunto previo debe archivarse solo una vez.");
    Assert(repo.Vigentes(1).Count == 1, "Debe quedar un solo conjunto vigente.");
}

static async Task ErrorTransaccional()
{
    var (service, repo, log) = NuevoCaso();
    repo.FallarAlInsertar = true;
    var result = await service.AprobarAsync(1, 10, "ana");
    Assert(!result.Exito, "El error simulado debe informarse.");
    Assert(repo.Vigentes(1).Count == 1 && repo.Vigentes(1)[0].DesgloseID == 50,
        "El rollback debe restaurar el conjunto anterior.");
    Assert(repo.Historial(1).Count == 0 && repo.Aprobacion(1) == null,
        "El rollback no debe dejar historial ni aprobación parcial.");
    Assert(log.Llamadas == 0, "La bitácora no debe registrar un reemplazo revertido.");
}

static async Task ErrorBitacora()
{
    var (service, repo, log) = NuevoCaso();
    log.Fallar = true;
    var result = await service.AprobarAsync(1, 10, "ana");
    Assert(!result.Exito, "El fallo de bitácora debe revertir la aprobación.");
    Assert(repo.Vigentes(1)[0].DesgloseID == 50 && repo.Aprobacion(1) == null,
        "No debe quedar ningún cambio si falla la bitácora.");
}

static async Task ActorNoAutenticado()
{
    var (service, repo, log) = NuevoCaso();
    var result = await service.AprobarAsync(1, 10, " ");
    Assert(!result.Exito && repo.Aprobacion(1) == null && log.Llamadas == 0,
        "Sin actor de sesión no debe iniciarse la aprobación.");
}

static (PropuestaService Service, FakePropuestaRepository Repo, FakeBitacora Log) NuevoCaso()
{
    var repo = new FakePropuestaRepository();
    repo.AgregarPropuesta(10, 1, [Linea(100, 10, "detalle propuesto")]);
    repo.DefinirVigentes(1, [new DesgloseAnterior
    {
        DesgloseID = 50, SolicitudID = 1, Mes = 5, Anio = 2025,
        Horas = 4, Monto = 120, IVA = 15.60m, Total = 135.60m,
        Observaciones = "desglose anterior", PorcentajeCobro = 100
    }]);
    var log = new FakeBitacora();
    var service = new PropuestaService(new FakeConnectionFactory(), repo, log,
        NullLogger<PropuestaService>.Instance);
    return (service, repo, log);
}

static DesglosePropuesta Linea(int id, int propuestaId, string observaciones) => new()
{
    DesglosePropuestaID = id, PropuestaID = propuestaId, Mes = 6, Anio = 2026,
    Horas = 8, Monto = 1000, IVA = 130, Total = 1130,
    Observaciones = observaciones, PorcentajeCobro = 100
};

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

sealed class FakeBitacora : IBitacoraService
{
    public int Llamadas { get; private set; }
    public string? Actor { get; private set; }
    public string? Modulo { get; private set; }
    public string? Accion { get; private set; }
    public bool Fallar { get; set; }

    public Task RegistrarAsync(string usuario, string modulo, string accion, object? datos = null)
        => Task.CompletedTask;

    public Task RegistrarAsync(string usuario, string modulo, string accion, object? datos,
        IDbConnection connection, IDbTransaction transaction)
    {
        if (Fallar) throw new InvalidOperationException("Fallo simulado de bitácora.");
        Llamadas++;
        Actor = usuario;
        Modulo = modulo;
        Accion = accion;
        return Task.CompletedTask;
    }
}

sealed class FakeConnectionFactory : IDbConnectionFactory
{
    public IDbConnection CreateConnection() => new FakeConnection();
}

sealed class FakeConnection : IDbConnection
{
    [AllowNull]
    string IDbConnection.ConnectionString { get => string.Empty; set { } }
    public int ConnectionTimeout => 0;
    public string Database => "fake";
    public ConnectionState State { get; private set; } = ConnectionState.Closed;
    public IDbTransaction BeginTransaction() => new FakeTransaction(this);
    public IDbTransaction BeginTransaction(IsolationLevel il) => new FakeTransaction(this, il);
    public void ChangeDatabase(string databaseName) { }
    public void Close() => State = ConnectionState.Closed;
    public IDbCommand CreateCommand() => throw new NotSupportedException();
    public void Open() => State = ConnectionState.Open;
    public void Dispose() => Close();
}

sealed class FakeTransaction : IDbTransaction
{
    private readonly List<Action> _commit = [];
    private readonly List<Action> _finish = [];
    private bool _finished;
    public FakeTransaction(IDbConnection connection, IsolationLevel isolation = IsolationLevel.ReadCommitted)
    { Connection = connection; IsolationLevel = isolation; }
    public IDbConnection Connection { get; }
    public IsolationLevel IsolationLevel { get; }
    public object? Get(object key) => _items.GetValueOrDefault(key);
    public void Set(object key, object value) => _items[key] = value;
    private readonly Dictionary<object, object> _items = new(ReferenceEqualityComparer.Instance);
    public void OnCommit(Action action) => _commit.Add(action);
    public void OnFinish(Action action) => _finish.Add(action);
    public void Commit()
    {
        if (_finished) return;
        foreach (var action in _commit) action();
        Finish();
    }
    public void Rollback() => Finish();
    private void Finish()
    {
        if (_finished) return;
        _finished = true;
        foreach (var action in _finish) action();
    }
    public void Dispose() => Rollback();
}

sealed class FakePropuestaRepository : IPropuestaRepository
{
    private readonly object _sync = new();
    private readonly HashSet<int> _solicitudes = [1, 2];
    private readonly Dictionary<int, PropuestaDetalle> _propuestas = [];
    private readonly Dictionary<int, List<DesgloseAnterior>> _vigentes = [];
    private readonly Dictionary<int, List<DesglosePropuestaHistorial>> _historial = [];
    private readonly Dictionary<int, AprobacionPropuesta> _aprobaciones = [];
    private readonly ConcurrentDictionary<int, SemaphoreSlim> _gates = new();
    private int _nextDesgloseId = 200;
    public bool FallarAlInsertar { get; set; }

    public void AgregarPropuesta(int id, int solicitudId, IReadOnlyList<DesglosePropuesta> lineas)
    {
        lock (_sync)
        {
            _propuestas[id] = new PropuestaDetalle
            {
                PropuestaID = id, SolicitudID = solicitudId, ProveedorID = 20,
                CedulaJuridica = "3-101-123456", NombreProveedor = "Proveedor de prueba",
                FechaPropuesta = new DateTime(2026, 11, 9), Aprobada = false,
                ConsecutivoOficio = "OF-1", TituloSolicitud = "Solicitud de prueba",
                Lineas = lineas
            };
        }
    }

    public void DefinirVigentes(int solicitudId, IReadOnlyList<DesgloseAnterior> desglose)
    { lock (_sync) _vigentes[solicitudId] = desglose.ToList(); }
    public IReadOnlyList<DesgloseAnterior> Vigentes(int id) { lock (_sync) return CloneRows(_vigentes.GetValueOrDefault(id) ?? []); }
    public IReadOnlyList<DesglosePropuestaHistorial> Historial(int id) { lock (_sync) return _historial.GetValueOrDefault(id)?.ToList() ?? []; }
    public AprobacionPropuesta? Aprobacion(int id) { lock (_sync) return _aprobaciones.GetValueOrDefault(id); }

    public Task<IReadOnlyList<PropuestaResumen>> ListarPorSolicitudAsync(int solicitudId)
    {
        lock (_sync)
        {
            IReadOnlyList<PropuestaResumen> rows = _propuestas.Values.Where(p => p.SolicitudID == solicitudId)
                .Select(p => new PropuestaResumen
                {
                    PropuestaID = p.PropuestaID, SolicitudID = p.SolicitudID, ProveedorID = p.ProveedorID,
                    CedulaJuridica = p.CedulaJuridica, NombreProveedor = p.NombreProveedor,
                    FechaPropuesta = p.FechaPropuesta, Aprobada = p.Aprobada,
                    SolicitudTieneAprobacion = _aprobaciones.ContainsKey(solicitudId)
                }).ToList();
            return Task.FromResult(rows);
        }
    }
    public Task<PropuestaDetalle?> ObtenerDetalleAsync(int solicitudId, int propuestaId)
    { lock (_sync) return Task.FromResult(_propuestas.TryGetValue(propuestaId, out var p) && p.SolicitudID == solicitudId ? CloneProposal(p) : null); }
    public Task<AprobacionPropuesta?> ObtenerAprobacionAsync(int solicitudId)
    { lock (_sync) return Task.FromResult(_aprobaciones.GetValueOrDefault(solicitudId)); }
    public Task<IReadOnlyList<DesglosePropuestaHistorial>> ListarHistorialAsync(int solicitudId)
    { lock (_sync) return Task.FromResult<IReadOnlyList<DesglosePropuestaHistorial>>(_historial.GetValueOrDefault(solicitudId)?.ToList() ?? []); }

    public async Task<bool> BloquearSolicitudAsync(IDbConnection connection, IDbTransaction transaction, int solicitudId)
    {
        if (!_solicitudes.Contains(solicitudId)) return false;
        var gate = _gates.GetOrAdd(solicitudId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        var tx = (FakeTransaction)transaction;
        var state = Snapshot(solicitudId);
        tx.Set(this, state);
        tx.OnCommit(() => Store(solicitudId, state));
        tx.OnFinish(() => gate.Release());
        return true;
    }

    public Task<PropuestaDetalle?> ObtenerDetalleAsync(IDbConnection connection, IDbTransaction transaction, int solicitudId, int propuestaId)
    {
        var state = Get(transaction);
        return Task.FromResult(state.Propuestas.TryGetValue(propuestaId, out var p) && p.SolicitudID == solicitudId ? CloneProposal(p) : null);
    }
    public Task<bool> BloquearPropuestaAsync(IDbConnection connection, IDbTransaction transaction, int solicitudId, int propuestaId)
    {
        var state = Get(transaction);
        return Task.FromResult(state.Propuestas.TryGetValue(propuestaId, out var proposal)
            && proposal.SolicitudID == solicitudId);
    }
    public Task<bool> TieneAprobacionAsync(IDbConnection connection, IDbTransaction transaction, int solicitudId)
        => Task.FromResult(Get(transaction).Aprobacion != null || Get(transaction).Propuestas.Values.Any(p => p.Aprobada));
    public Task<IReadOnlyList<DesgloseAnterior>> ListarDesglosesVigentesAsync(IDbConnection connection, IDbTransaction transaction, int solicitudId)
        => Task.FromResult<IReadOnlyList<DesgloseAnterior>>(CloneRows(Get(transaction).Vigentes));
    public Task<int> EliminarDesglosesVigentesAsync(IDbConnection connection, IDbTransaction transaction, int solicitudId)
    {
        var state = Get(transaction); var count = state.Vigentes.Count; state.Vigentes.Clear(); return Task.FromResult(count);
    }
    public Task<int> InsertarDesglosesPropuestosAsync(IDbConnection connection, IDbTransaction transaction, int solicitudId, int propuestaId)
    {
        if (FallarAlInsertar) throw new InvalidOperationException("Fallo simulado durante el reemplazo.");
        var state = Get(transaction);
        var proposal = state.Propuestas[propuestaId];
        foreach (var line in proposal.Lineas)
            state.Vigentes.Add(new DesgloseAnterior
            {
                DesgloseID = Interlocked.Increment(ref _nextDesgloseId), SolicitudID = solicitudId,
                DesglosePropuestaID = line.DesglosePropuestaID, PropuestaID = propuestaId,
                Mes = line.Mes, Anio = line.Anio, Horas = line.Horas, Monto = line.Monto,
                IVA = line.IVA, Total = line.Total, Observaciones = line.Observaciones,
                PorcentajeCobro = line.PorcentajeCobro
            });
        return Task.FromResult(proposal.Lineas.Count);
    }
    public Task<IReadOnlyList<DesgloseAnterior>> ListarDesglosesVigentesTrasAprobacionAsync(IDbConnection connection, IDbTransaction transaction, int solicitudId)
        => Task.FromResult<IReadOnlyList<DesgloseAnterior>>(CloneRows(Get(transaction).Vigentes));
    public Task<int> RegistrarAprobacionAsync(IDbConnection connection, IDbTransaction transaction, AprobacionPropuesta aprobacion)
    { var state = Get(transaction); aprobacion.AprobacionID = 1; state.Aprobacion = aprobacion; return Task.FromResult(1); }
    public Task<int> ArchivarDesglosesAsync(IDbConnection connection, IDbTransaction transaction, int aprobacionId, IReadOnlyList<DesgloseAnterior> desgloses)
    {
        Get(transaction).Historial.AddRange(desgloses.Select(d => new DesglosePropuestaHistorial
        {
            HistorialDesgloseID = Get(transaction).Historial.Count + 1, AprobacionID = aprobacionId,
            DesgloseID = d.DesgloseID, SolicitudID = d.SolicitudID, DesglosePropuestaID = d.DesglosePropuestaID,
            PropuestaID = d.PropuestaID, Mes = d.Mes, Anio = d.Anio, Horas = d.Horas, Monto = d.Monto,
            IVA = d.IVA, Total = d.Total, Observaciones = d.Observaciones, PorcentajeCobro = d.PorcentajeCobro
        }));
        return Task.FromResult(desgloses.Count);
    }
    public Task<int> MarcarAprobadaAsync(IDbConnection connection, IDbTransaction transaction, int solicitudId, int propuestaId)
    { var state = Get(transaction); if (!state.Propuestas.TryGetValue(propuestaId, out var p) || p.SolicitudID != solicitudId || p.Aprobada) return Task.FromResult(0); p.Aprobada = true; return Task.FromResult(1); }

    private State Get(IDbTransaction transaction) => (State)((FakeTransaction)transaction).Get(this)!;
    private State Snapshot(int solicitudId)
    {
        lock (_sync)
        {
            return new State
            {
                Propuestas = _propuestas.Where(kv => kv.Value.SolicitudID == solicitudId).ToDictionary(kv => kv.Key, kv => CloneProposal(kv.Value)),
                Vigentes = CloneRows(_vigentes.GetValueOrDefault(solicitudId) ?? []),
                Historial = _historial.GetValueOrDefault(solicitudId)?.ToList() ?? [],
                Aprobacion = _aprobaciones.GetValueOrDefault(solicitudId)
            };
        }
    }
    private void Store(int solicitudId, State state)
    {
        lock (_sync)
        {
            foreach (var key in _propuestas.Where(kv => kv.Value.SolicitudID == solicitudId).Select(kv => kv.Key).ToList()) _propuestas.Remove(key);
            foreach (var pair in state.Propuestas) _propuestas[pair.Key] = CloneProposal(pair.Value);
            _vigentes[solicitudId] = CloneRows(state.Vigentes);
            _historial[solicitudId] = state.Historial.ToList();
            if (state.Aprobacion is not null) _aprobaciones[solicitudId] = state.Aprobacion;
        }
    }
    private static List<DesgloseAnterior> CloneRows(IEnumerable<DesgloseAnterior> rows) => rows.Select(d => new DesgloseAnterior
    {
        DesgloseID = d.DesgloseID, SolicitudID = d.SolicitudID, DesglosePropuestaID = d.DesglosePropuestaID,
        PropuestaID = d.PropuestaID, Mes = d.Mes, Anio = d.Anio, Horas = d.Horas, Monto = d.Monto,
        IVA = d.IVA, Total = d.Total, Observaciones = d.Observaciones, PorcentajeCobro = d.PorcentajeCobro
    }).ToList();
    private static PropuestaDetalle CloneProposal(PropuestaDetalle p) => new()
    {
        PropuestaID = p.PropuestaID, SolicitudID = p.SolicitudID, ProveedorID = p.ProveedorID,
        CedulaJuridica = p.CedulaJuridica, NombreProveedor = p.NombreProveedor,
        FechaPropuesta = p.FechaPropuesta, Aprobada = p.Aprobada,
        SolicitudTieneAprobacion = p.SolicitudTieneAprobacion,
        ConsecutivoOficio = p.ConsecutivoOficio, TituloSolicitud = p.TituloSolicitud,
        Lineas = p.Lineas.Select(l => new DesglosePropuesta
        {
            DesglosePropuestaID = l.DesglosePropuestaID, PropuestaID = l.PropuestaID,
            Mes = l.Mes, Anio = l.Anio, Horas = l.Horas, Monto = l.Monto,
            IVA = l.IVA, Total = l.Total, Observaciones = l.Observaciones, PorcentajeCobro = l.PorcentajeCobro
        }).ToList()
    };

    private sealed class State
    {
        public Dictionary<int, PropuestaDetalle> Propuestas { get; init; } = [];
        public List<DesgloseAnterior> Vigentes { get; init; } = [];
        public List<DesglosePropuestaHistorial> Historial { get; init; } = [];
        public AprobacionPropuesta? Aprobacion { get; set; }
    }
}

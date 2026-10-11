using System.Data;
using System.Text.Json;
using AdministracionSoluciones.Entities;
using AdministracionSoluciones.Repository;
using AdministracionSoluciones.Services;
using Dapper;
using Microsoft.Extensions.Logging.Abstractions;
using MySqlConnector;

// Exclusivamente una instancia temporal de MySQL, nunca la base del usuario.
var connection = Environment.GetEnvironmentVariable("CATALOGOS_TEST_CONNECTION")
    ?? throw new InvalidOperationException("Defina CATALOGOS_TEST_CONNECTION para una base temporal.");
var factory = new TestFactory(connection);
var repository = new CatalogoRepository(factory);
var service = new CatalogoService(repository, new BitacoraService(new BitacoraRepository(factory)), NullLogger<CatalogoService>.Instance);
using var db = factory.CreateConnection();
var pruebas = 0;
void Check(bool condition, string description) {
    if (!condition) throw new Exception("FALLÓ: " + description);
    Console.WriteLine("OK: " + description); pruebas++;
}
var estados = await service.ListarAsync(TipoCatalogo.Estados, 1, "pruebas");
Check(estados.Total == 5 && estados.Items.Select(x => x.Nombre).ToHashSet().SetEquals(new[] { "Nueva", "Respondida", "Iniciada", "Finalizada", "Vencida" }), "Cinco estados iniciales");
Check(!(await service.GuardarAsync(TipoCatalogo.Representantes, new() { Nombre = " ", Email = "a@b.com" }, "pruebas")).Exito, "Rechazo de nombre vacío");
Check(!(await service.GuardarAsync(TipoCatalogo.Representantes, new() { Nombre = "Ana", Email = "inválido" }, "pruebas")).Exito, "Rechazo de correo inválido");
Check(!(await service.GuardarAsync(TipoCatalogo.Estados, new() { Nombre = "Estado", Identificador = " " }, "pruebas")).Exito, "Identificador requerido");
for (var i = 0; i < 12; i++) Check((await service.GuardarAsync(TipoCatalogo.Representantes, new() { Nombre = $"Persona {i:00}", Email = $"persona{i}@ejemplo.com" }, "pruebas")).Exito, $"Crear representante {i}");
var primera = await service.ListarAsync(TipoCatalogo.Representantes, 1, "pruebas");
var segunda = await service.ListarAsync(TipoCatalogo.Representantes, 2, "pruebas");
Check(primera.Items.Count == 10 && segunda.Items.Count == 2 && primera.Total == 12, "Paginación de representantes 10 + 2");
Check(!primera.Items.Select(x => x.Id).Intersect(segunda.Items.Select(x => x.Id)).Any(), "Páginas sin registros repetidos");
Check((await service.ListarAsync(TipoCatalogo.Representantes, -5, "pruebas")).Pagina == 1 && (await service.ListarAsync(TipoCatalogo.Representantes, 999, "pruebas")).Pagina == 2, "Límites de paginación");
var persona = primera.Items[0];
persona.Nombre = "Nombre actualizado";
Check((await service.GuardarAsync(TipoCatalogo.Representantes, persona, "pruebas")).Exito && (await service.ObtenerAsync(TipoCatalogo.Representantes, persona.Id))!.Nombre == persona.Nombre, "Editar representante");
var detalle = await db.QuerySingleAsync<string>("SELECT Detalle FROM bitacora WHERE Modulo='Representantes' AND Accion='Actualizar' ORDER BY BitacoraID DESC LIMIT 1");
using (var json = JsonDocument.Parse(detalle)) Check(json.RootElement.GetProperty("Anterior").GetProperty("Nombre").GetString() == "Persona 00" && json.RootElement.GetProperty("Actual").GetProperty("Nombre").GetString() == persona.Nombre, "Bitácora conserva datos anteriores y actuales");
Check(await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM bitacora WHERE Accion='El usuario consulta Representantes' AND Detalle IS NULL") > 0, "Bitácora de consultas sin JSON");
var estado = new CatalogoItem { Nombre = "En revisión", Identificador = "REVISION" };
Check((await service.GuardarAsync(TipoCatalogo.Estados, estado, "pruebas")).Exito, "Crear estado");
Check(!(await service.GuardarAsync(TipoCatalogo.Estados, new() { Nombre = "Duplicado", Identificador = "REVISION" }, "pruebas")).Exito, "Identificador único");
estado.Nombre = "En evaluación";
Check((await service.GuardarAsync(TipoCatalogo.Estados, estado, "pruebas")).Exito && (await service.ObtenerAsync(TipoCatalogo.Estados, estado.Id))!.Nombre == estado.Nombre, "Editar estado");
await db.ExecuteAsync("CREATE TABLE solicitudes_prueba (Id INT PRIMARY KEY, RepresentanteID INT, EstadoSolicitudID INT, FOREIGN KEY (RepresentanteID) REFERENCES representantes(RepresentanteID) ON DELETE RESTRICT, FOREIGN KEY (EstadoSolicitudID) REFERENCES estados_solicitud(EstadoSolicitudID) ON DELETE RESTRICT)");
await db.ExecuteAsync("INSERT INTO solicitudes_prueba VALUES (1, @RepresentanteID, @EstadoSolicitudID)", new { RepresentanteID = persona.Id, EstadoSolicitudID = estado.Id });
foreach (var caso in new[] { (TipoCatalogo.Representantes, persona.Id), (TipoCatalogo.Estados, estado.Id) }) {
    var resultado = await service.GuardarAsync(caso.Item1, new() { Id = caso.Id }, "pruebas", true);
    Check(!resultado.Exito && resultado.Mensaje == "No se puede eliminar un registro con datos relacionados.", "Bloqueo de eliminación relacionada: " + caso.Item1);
}
await db.ExecuteAsync("DELETE FROM solicitudes_prueba");
Check((await service.GuardarAsync(TipoCatalogo.Representantes, persona, "pruebas", true)).Exito && await service.ObtenerAsync(TipoCatalogo.Representantes, persona.Id) == null, "Eliminar representante sin relaciones");
Check((await service.GuardarAsync(TipoCatalogo.Estados, estado, "pruebas", true)).Exito && await service.ObtenerAsync(TipoCatalogo.Estados, estado.Id) == null, "Eliminar estado sin relaciones");
Check(!(await service.GuardarAsync(TipoCatalogo.Estados, estado, "pruebas", true)).Exito, "Registro inexistente");
for (var i = 0; i < 7; i++) await service.GuardarAsync(TipoCatalogo.Estados, new() { Nombre = $"Extra {i}", Identificador = $"EXTRA{i}" }, "pruebas");
Check((await service.ListarAsync(TipoCatalogo.Estados, 1, "pruebas")).Items.Count == 10 && (await service.ListarAsync(TipoCatalogo.Estados, 2, "pruebas")).Items.Count == 2, "Paginación de estados 10 + 2");
await db.ExecuteAsync("RENAME TABLE bitacora TO bitacora_temporal");
Check(!(await service.GuardarAsync(TipoCatalogo.Representantes, new() { Nombre = "No persistir", Email = "test@ejemplo.com" }, "pruebas")).Exito, "Fallo de bitácora reportado");
Check(await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM representantes WHERE Nombre='No persistir'") == 0, "Cambio revertido cuando falla la bitácora");
await db.ExecuteAsync("RENAME TABLE bitacora_temporal TO bitacora");
Console.WriteLine($"TOTAL: {pruebas} comprobaciones correctas.");

class TestFactory(string connection) : IDbConnectionFactory {
    public IDbConnection CreateConnection() => new MySqlConnection(connection);
}

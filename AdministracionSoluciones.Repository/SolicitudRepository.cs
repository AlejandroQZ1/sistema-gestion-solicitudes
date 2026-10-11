using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdministracionSoluciones.Entities;
using Dapper;

namespace AdministracionSoluciones.Repository
{
    public class SolicitudRepository : ISolicitudRepository
    {
        private const int RegistrosPorPagina = 5;
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public SolicitudRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<(IEnumerable<Solicitud> Items, int Total, int Pagina)> ListarAsync(int pagina)
        {
            using var connection = _dbConnectionFactory.CreateConnection();

            const string sqlTotal = "SELECT COUNT(*) FROM solicitudes;";
            var total = await connection.ExecuteScalarAsync<int>(sqlTotal);

            var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)RegistrosPorPagina));
            pagina = Math.Clamp(pagina, 1, totalPaginas);
            var offset = (pagina - 1) * RegistrosPorPagina;

            const string sql = @"
                SELECT
                    s.SolicitudID,
                    s.ConsecutivoOficio,
                    s.DocumentoRespuesta,
                    s.DocumentoInicio,
                    s.Titulo,
                    s.Descripcion,
                    s.RepresentanteID,
                    s.Observaciones,
                    s.EstadoSolicitudID,
                    s.FechaIngreso,
                    s.FechaRespuesta,
                    s.FechaInicio,
                    r.Nombre AS NombreRepresentante,
                    r.Email AS EmailRepresentante,
                    e.Nombre AS NombreEstado,
                    e.Identificador AS IdentificadorEstado,
                    CASE
                        WHEN s.FechaRespuesta IS NULL
                             AND DATEDIFF(CURDATE(), DATE(s.FechaIngreso)) = 0
                        THEN 1
                        ELSE 0
                    END AS VenceHoy
                FROM solicitudes s
                LEFT JOIN representantes r
                    ON r.RepresentanteID = s.RepresentanteID
                INNER JOIN estados_solicitud e
                    ON e.EstadoSolicitudID = s.EstadoSolicitudID
                ORDER BY s.SolicitudID DESC
                LIMIT @Limite OFFSET @Offset;";

            var items = await connection.QueryAsync<Solicitud>(
                sql,
                new { Limite = RegistrosPorPagina, Offset = offset });

            return (items, total, pagina);
        }

        public async Task<Solicitud?> ObtenerAsync(int id)
        {
            using var connection = _dbConnectionFactory.CreateConnection();

            const string sql = @"
                SELECT
                    s.SolicitudID,
                    s.ConsecutivoOficio,
                    s.DocumentoRespuesta,
                    s.DocumentoInicio,
                    s.Titulo,
                    s.Descripcion,
                    s.RepresentanteID,
                    s.Observaciones,
                    s.EstadoSolicitudID,
                    s.FechaIngreso,
                    s.FechaRespuesta,
                    s.FechaInicio,
                    r.Nombre AS NombreRepresentante,
                    r.Email AS EmailRepresentante,
                    e.Nombre AS NombreEstado,
                    e.Identificador AS IdentificadorEstado,
                    CASE
                        WHEN s.FechaRespuesta IS NULL
                             AND DATEDIFF(CURDATE(), DATE(s.FechaIngreso)) = 0
                        THEN 1
                        ELSE 0
                    END AS VenceHoy
                FROM solicitudes s
                LEFT JOIN representantes r
                    ON r.RepresentanteID = s.RepresentanteID
                INNER JOIN estados_solicitud e
                    ON e.EstadoSolicitudID = s.EstadoSolicitudID
                WHERE s.SolicitudID = @Id
                LIMIT 1;";

            return await connection.QuerySingleOrDefaultAsync<Solicitud>(
                sql, new { Id = id });
        }

        public async Task<int> CrearAsync(Solicitud solicitud)
        {
            using var connection = _dbConnectionFactory.CreateConnection();

            const string sql = @"
                INSERT INTO solicitudes
                (
                    ConsecutivoOficio,
                    DocumentoRespuesta,
                    DocumentoInicio,
                    Titulo,
                    Descripcion,
                    RepresentanteID,
                    Observaciones,
                    EstadoSolicitudID,
                    FechaIngreso,
                    FechaRespuesta,
                    FechaInicio
                )
                VALUES
                (
                    @ConsecutivoOficio,
                    @DocumentoRespuesta,
                    @DocumentoInicio,
                    @Titulo,
                    @Descripcion,
                    @RepresentanteID,
                    @Observaciones,
                    @EstadoSolicitudID,
                    @FechaIngreso,
                    @FechaRespuesta,
                    @FechaInicio
                );
                SELECT LAST_INSERT_ID();";

            return await connection.ExecuteScalarAsync<int>(sql, solicitud);
        }

        public async Task<bool> ActualizarAsync(
            Solicitud solicitud,
            int estadoAnteriorId)
        {
            using var connection = _dbConnectionFactory.CreateConnection();

            const string sql = @"
                UPDATE solicitudes
                SET
                    ConsecutivoOficio = @ConsecutivoOficio,
                    DocumentoRespuesta = @DocumentoRespuesta,
                    DocumentoInicio = @DocumentoInicio,
                    Titulo = @Titulo,
                    Descripcion = @Descripcion,
                    RepresentanteID = @RepresentanteID,
                    Observaciones = @Observaciones,
                    EstadoSolicitudID = @EstadoSolicitudID,
                    FechaIngreso = @FechaIngreso,
                    FechaRespuesta = @FechaRespuesta,
                    FechaInicio = @FechaInicio
                WHERE SolicitudID = @SolicitudID
                  AND EstadoSolicitudID = @EstadoAnteriorID;";

            var filasAfectadas = await connection.ExecuteAsync(
                sql,
                new
                {
                    solicitud.ConsecutivoOficio,
                    solicitud.DocumentoRespuesta,
                    solicitud.DocumentoInicio,
                    solicitud.Titulo,
                    solicitud.Descripcion,
                    solicitud.RepresentanteID,
                    solicitud.Observaciones,
                    solicitud.EstadoSolicitudID,
                    solicitud.FechaIngreso,
                    solicitud.FechaRespuesta,
                    solicitud.FechaInicio,
                    solicitud.SolicitudID,
                    EstadoAnteriorID = estadoAnteriorId
                });
            return filasAfectadas > 0;
        }

        public async Task<bool> ActualizarEstadoAsync(
            int solicitudId,
            int estadoAnteriorId,
            int estadoNuevoId)
        {
            using var connection = _dbConnectionFactory.CreateConnection();

            const string sql = @"
                UPDATE solicitudes
                SET EstadoSolicitudID = @EstadoNuevoID
                WHERE SolicitudID = @SolicitudID
                  AND EstadoSolicitudID = @EstadoAnteriorID
                  AND FechaRespuesta IS NULL;";

            var filasAfectadas = await connection.ExecuteAsync(
                sql,
                new
                {
                    SolicitudID = solicitudId,
                    EstadoAnteriorID = estadoAnteriorId,
                    EstadoNuevoID = estadoNuevoId
                });

            return filasAfectadas > 0;
        }

        public async Task<bool> EliminarAsync(int id)
        {
            using var connection = _dbConnectionFactory.CreateConnection();

            const string sql =
                "DELETE FROM solicitudes WHERE SolicitudID = @Id;";

            var filasAfectadas = await connection.ExecuteAsync(
                sql, new { Id = id });

            return filasAfectadas > 0;
        }

        public async Task<int?> ObtenerIdEstadoAsync(string identificador)
        {
            using var connection = _dbConnectionFactory.CreateConnection();

            const string sql = @"
                SELECT EstadoSolicitudID
                FROM estados_solicitud
                WHERE Identificador = @Identificador
                LIMIT 1;";

            return await connection.QuerySingleOrDefaultAsync<int?>(
                sql, new { Identificador = identificador });
        }

        public async Task<IEnumerable<Solicitud>> ListarPendientesDeVencimientoAsync()
        {
            using var connection = _dbConnectionFactory.CreateConnection();

            const string sql = @"
                SELECT
                    s.SolicitudID,
                    s.ConsecutivoOficio,
                    s.DocumentoRespuesta,
                    s.DocumentoInicio,
                    s.Titulo,
                    s.Descripcion,
                    s.RepresentanteID,
                    s.Observaciones,
                    s.EstadoSolicitudID,
                    s.FechaIngreso,
                    s.FechaRespuesta,
                    s.FechaInicio,
                    r.Nombre AS NombreRepresentante,
                    r.Email AS EmailRepresentante,
                    e.Nombre AS NombreEstado,
                    e.Identificador AS IdentificadorEstado,
                    0 AS VenceHoy
                FROM solicitudes s
                LEFT JOIN representantes r
                    ON r.RepresentanteID = s.RepresentanteID
                INNER JOIN estados_solicitud e
                    ON e.EstadoSolicitudID = s.EstadoSolicitudID
                WHERE s.FechaRespuesta IS NULL;";

            return await connection.QueryAsync<Solicitud>(sql);
        }
    }
}

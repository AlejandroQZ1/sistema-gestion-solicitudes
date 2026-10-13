using AdministracionSoluciones.Entities;
using Dapper;

namespace AdministracionSoluciones.Repository
{
    /// <summary>
    /// Repositorio de acceso a datos para Desgloses de Solicitudes (SOL2).
    /// Utiliza Dapper sobre MySQL.
    /// </summary>
    public class DesgloseRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public DesgloseRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<IEnumerable<Desglose>> GetAllAsync(int? solicitudId = null)
        {
            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                var sql = @"
                    SELECT
                        d.DesgloseID,
                        d.SolicitudID,
                        d.Mes,
                        d.Anio,
                        d.Horas,
                        d.Monto,
                        d.IVA,
                        d.Total,
                        d.Observaciones,
                        d.PorcentajeCobro,
                        s.ConsecutivoOficio,
                        s.Titulo AS TituloSolicitud
                    FROM desgloses d
                    INNER JOIN solicitudes s ON d.SolicitudID = s.SolicitudID
                    WHERE (@SolicitudID IS NULL OR d.SolicitudID = @SolicitudID)
                    ORDER BY d.Anio DESC, d.Mes DESC, d.DesgloseID DESC";

                return await connection.QueryAsync<Desglose>(sql, new { SolicitudID = solicitudId });
            }
        }

        public async Task<Desglose?> GetByIdAsync(int id)
        {
            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                var sql = @"
                    SELECT
                        d.DesgloseID,
                        d.SolicitudID,
                        d.Mes,
                        d.Anio,
                        d.Horas,
                        d.Monto,
                        d.IVA,
                        d.Total,
                        d.Observaciones,
                        d.PorcentajeCobro,
                        s.ConsecutivoOficio,
                        s.Titulo AS TituloSolicitud
                    FROM desgloses d
                    INNER JOIN solicitudes s ON d.SolicitudID = s.SolicitudID
                    WHERE d.DesgloseID = @Id";

                return await connection.QueryFirstOrDefaultAsync<Desglose>(sql, new { Id = id });
            }
        }

        // IVA y Total son columnas calculadas por la base de datos.
        public async Task<int> InsertAsync(Desglose desglose)
        {
            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                var sql = @"
                    INSERT INTO desgloses
                        (SolicitudID, Mes, Anio, Horas, Monto, Observaciones, PorcentajeCobro)
                    VALUES
                        (@SolicitudID, @Mes, @Anio, @Horas, @Monto, @Observaciones, @PorcentajeCobro);
                    SELECT LAST_INSERT_ID();";

                return await connection.ExecuteScalarAsync<int>(sql, desglose);
            }
        }

        public async Task<int> UpdateAsync(Desglose desglose)
        {
            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                var sql = @"
                    UPDATE desgloses
                    SET
                        SolicitudID = @SolicitudID,
                        Mes = @Mes,
                        Anio = @Anio,
                        Horas = @Horas,
                        Monto = @Monto,
                        Observaciones = @Observaciones,
                        PorcentajeCobro = @PorcentajeCobro
                    WHERE DesgloseID = @DesgloseID";

                return await connection.ExecuteAsync(sql, desglose);
            }
        }

        public async Task<int> DeleteAsync(int id)
        {
            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                return await connection.ExecuteAsync("DELETE FROM desgloses WHERE DesgloseID = @Id", new { Id = id });
            }
        }

        public async Task<IEnumerable<Solicitud>> GetSolicitudesParaComboAsync()
        {
            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                return await connection.QueryAsync<Solicitud>(
                    @"SELECT s.SolicitudID, s.ConsecutivoOficio, s.Titulo,
                             s.EstadoSolicitudID, e.Nombre AS NombreEstado
                      FROM solicitudes s
                      INNER JOIN estados_solicitud e ON e.EstadoSolicitudID = s.EstadoSolicitudID
                      ORDER BY s.ConsecutivoOficio ASC");
            }
        }

        public async Task<Solicitud?> GetSolicitudByIdAsync(int solicitudId)
        {
            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                return await connection.QueryFirstOrDefaultAsync<Solicitud>(
                    @"SELECT s.*, e.Nombre AS NombreEstado
                      FROM solicitudes s
                      INNER JOIN estados_solicitud e ON e.EstadoSolicitudID = s.EstadoSolicitudID
                      WHERE s.SolicitudID = @SolicitudID", new { SolicitudID = solicitudId });
            }
        }
    }
}

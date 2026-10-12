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
                        CASE 
                            WHEN d.Asignado = 1 THEN 1
                            WHEN s.Estado IN ('Iniciada', 'Respondida', 'Finalizada') THEN 1
                            WHEN s.RepresentanteID IS NOT NULL THEN 1
                            ELSE 0
                        END AS Asignado,
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
                        CASE 
                            WHEN d.Asignado = 1 THEN 1
                            WHEN s.Estado IN ('Iniciada', 'Respondida', 'Finalizada') THEN 1
                            WHEN s.RepresentanteID IS NOT NULL THEN 1
                            ELSE 0
                        END AS Asignado,
                        s.ConsecutivoOficio,
                        s.Titulo AS TituloSolicitud
                    FROM desgloses d
                    INNER JOIN solicitudes s ON d.SolicitudID = s.SolicitudID
                    WHERE d.DesgloseID = @Id";

                return await connection.QueryFirstOrDefaultAsync<Desglose>(sql, new { Id = id });
            }
        }

        public async Task<int> InsertAsync(Desglose desglose)
        {
            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                var sql = @"
                    INSERT INTO desgloses 
                        (SolicitudID, Mes, Anio, Horas, Monto, IVA, Total, Observaciones, PorcentajeCobro, Asignado)
                    VALUES 
                        (@SolicitudID, @Mes, @Anio, @Horas, @Monto, @IVA, @Total, @Observaciones, @PorcentajeCobro, @Asignado);
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
                        IVA = @IVA,
                        Total = @Total,
                        Observaciones = @Observaciones,
                        PorcentajeCobro = @PorcentajeCobro,
                        Asignado = @Asignado
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

        /// <summary>
        /// Valida si el desglose ya está marcado como asignado o con registros relacionados.
        /// Criterio SOL2: Solo se puede eliminar un registro si no ha sido asignado.
        /// Se considera asignado si el desglose tiene Asignado = 1 o si la solicitud ya está iniciada/asignada.
        /// </summary>
        public async Task<bool> TieneDatosRelacionadosAsync(int id)
        {
            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                var sql = @"
                    SELECT 
                        CASE 
                            WHEN d.Asignado = 1 THEN 1
                            WHEN s.Estado IN ('Iniciada', 'Respondida', 'Finalizada') THEN 1
                            WHEN s.RepresentanteID IS NOT NULL THEN 1
                            ELSE 0
                        END
                    FROM desgloses d
                    INNER JOIN solicitudes s ON d.SolicitudID = s.SolicitudID
                    WHERE d.DesgloseID = @Id";

                var asignado = await connection.ExecuteScalarAsync<int?>(sql, new { Id = id });
                return (asignado ?? 0) == 1;
            }
        }

        public async Task<IEnumerable<Solicitud>> GetSolicitudesParaComboAsync()
        {
            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                return await connection.QueryAsync<Solicitud>(
                    "SELECT SolicitudID, ConsecutivoOficio, Titulo, Estado FROM solicitudes ORDER BY ConsecutivoOficio ASC");
            }
        }

        public async Task<Solicitud?> GetSolicitudByIdAsync(int solicitudId)
        {
            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                return await connection.QueryFirstOrDefaultAsync<Solicitud>(
                    "SELECT * FROM solicitudes WHERE SolicitudID = @SolicitudID", new { SolicitudID = solicitudId });
            }
        }
    }
}

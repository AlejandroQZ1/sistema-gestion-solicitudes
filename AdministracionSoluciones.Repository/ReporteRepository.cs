using AdministracionSoluciones.Entities;
using Dapper;

namespace AdministracionSoluciones.Repository
{
    /// <summary>
    /// Repositorio para la generación de reportes ejecutivos (SOL7).
    /// </summary>
    public class ReporteRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public ReporteRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        /// <summary>
        /// SOL7: Obtiene el monto que se facturará por mes entre todas las solicitudes.
        /// </summary>
        public async Task<IEnumerable<ReporteMontoFacturacionItem>> ObtenerReporteMontosFacturacionAsync(int mes, int anio)
        {
            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                var sql = @"
                    SELECT
                        s.ConsecutivoOficio,
                        s.Titulo,
                        d.Mes,
                        d.Anio,
                        SUM(d.Monto) AS Monto,
                        SUM(d.IVA) AS IVA,
                        SUM(d.Total) AS Total
                    FROM desgloses d
                    INNER JOIN solicitudes s ON d.SolicitudID = s.SolicitudID
                    WHERE d.Mes = @Mes AND d.Anio = @Anio
                    GROUP BY s.SolicitudID, s.ConsecutivoOficio, s.Titulo, d.Mes, d.Anio
                    ORDER BY s.ConsecutivoOficio ASC";

                return await connection.QueryAsync<ReporteMontoFacturacionItem>(sql, new { Mes = mes, Anio = anio });
            }
        }
    }
}

using AdministracionSoluciones.Entities;
using Dapper;

namespace AdministracionSoluciones.Repository
{
    /// <summary>Consultas SQL de la tabla "bitacora".</summary>
    public class BitacoraRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public BitacoraRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<int> InsertAsync(Bitacora bitacora)
        {
            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                var sql = @"INSERT INTO bitacora (Fecha, Usuario, Modulo, Accion, Detalle)
                            VALUES (@Fecha, @Usuario, @Modulo, @Accion, @Detalle)";
                return await connection.ExecuteAsync(sql, bitacora);
            }
        }
    }
}

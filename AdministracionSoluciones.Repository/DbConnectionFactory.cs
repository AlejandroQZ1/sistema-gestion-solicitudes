using System.Data;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace AdministracionSoluciones.Repository
{
    /// <summary>
    /// Crea las conexiones a MySQL usando la cadena "ConnectionStrings:DefaultConnection" de appsettings.json.
    /// </summary>
    public class DbConnectionFactory : IDbConnectionFactory
    {
        private readonly IConfiguration _configuration;

        public DbConnectionFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IDbConnection CreateConnection()
        {
            var connectionString =
                _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Falta la configuración externa ConnectionStrings:DefaultConnection.");
            }

            return new MySqlConnection(connectionString);
        }
    }
}

using System.Data;

namespace AdministracionSoluciones.Repository
{
    public interface IDbConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}

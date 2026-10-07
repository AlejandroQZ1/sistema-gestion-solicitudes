using AdministracionSoluciones.Entities;
using Dapper;

namespace AdministracionSoluciones.Repository
{
    /// <summary>Consultas SQL de la tabla "usuarios" (USR1, USR4).</summary>
    public class UsuarioRepository
    {
        private const string Columnas = "UsuarioID, NombreUsuario, NombreCompleto, Correo, Contrasena, Estado, IntentosFallidos";

        private readonly IDbConnectionFactory _dbConnectionFactory;

        public UsuarioRepository(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<IEnumerable<Usuario>> GetAllAsync()
        {
            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                return await connection.QueryAsync<Usuario>($"SELECT {Columnas} FROM usuarios ORDER BY NombreUsuario");
            }
        }

        public async Task<Usuario?> GetByIdAsync(int id)
        {
            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                return await connection.QuerySingleOrDefaultAsync<Usuario>(
                    $"SELECT {Columnas} FROM usuarios WHERE UsuarioID = @Id", new { Id = id });
            }
        }

        public async Task<Usuario?> GetByNombreUsuarioAsync(string nombreUsuario)
        {
            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                return await connection.QuerySingleOrDefaultAsync<Usuario>(
                    $"SELECT {Columnas} FROM usuarios WHERE NombreUsuario = @NombreUsuario", new { NombreUsuario = nombreUsuario });
            }
        }

        /// <summary>Indica si ya existe otro usuario con ese nombre de usuario (sin contar al usuario excluirId).</summary>
        public async Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario, int excluirId)
        {
            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                var cantidad = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM usuarios WHERE NombreUsuario = @NombreUsuario AND UsuarioID <> @Id",
                    new { NombreUsuario = nombreUsuario, Id = excluirId });
                return cantidad > 0;
            }
        }

        public async Task<int> CountAsync()
        {
            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                return await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM usuarios");
            }
        }

        /// <summary>Inserta el usuario y devuelve el Id generado.</summary>
        public async Task<int> InsertAsync(Usuario usuario)
        {
            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                var sql = @"INSERT INTO usuarios (NombreUsuario, NombreCompleto, Correo, Contrasena, Estado, IntentosFallidos)
                            VALUES (@NombreUsuario, @NombreCompleto, @Correo, @Contrasena, @Estado, @IntentosFallidos);
                            SELECT LAST_INSERT_ID();";
                return await connection.ExecuteScalarAsync<int>(sql, usuario);
            }
        }

        public async Task<int> UpdateAsync(Usuario usuario)
        {
            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                var sql = @"UPDATE usuarios
                            SET NombreUsuario = @NombreUsuario, NombreCompleto = @NombreCompleto, Correo = @Correo,
                                Contrasena = @Contrasena, Estado = @Estado, IntentosFallidos = @IntentosFallidos
                            WHERE UsuarioID = @UsuarioID";
                return await connection.ExecuteAsync(sql, usuario);
            }
        }

        /// <summary>Actualiza solo el estado y los intentos fallidos (login y botones del listado).</summary>
        public async Task<int> UpdateEstadoAsync(int id, string estado, int intentosFallidos)
        {
            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                var sql = "UPDATE usuarios SET Estado = @Estado, IntentosFallidos = @IntentosFallidos WHERE UsuarioID = @Id";
                return await connection.ExecuteAsync(sql, new { Id = id, Estado = estado, IntentosFallidos = intentosFallidos });
            }
        }

        public async Task<int> DeleteAsync(int id)
        {
            using (var connection = _dbConnectionFactory.CreateConnection())
            {
                return await connection.ExecuteAsync("DELETE FROM usuarios WHERE UsuarioID = @Id", new { Id = id });
            }
        }

        /// <summary>
        /// Indica si el usuario ya fue asignado en otra parte del sistema.
        /// PENDIENTE: cuando existan las tablas de los demás módulos, agregar aquí la consulta, por ejemplo:
        ///   SELECT (SELECT COUNT(*) FROM solicitudes WHERE UsuarioID = @Id)
        ///        + (SELECT COUNT(*) FROM tareas WHERE ResponsableID = @Id)
        /// </summary>
        public Task<bool> TieneDatosRelacionadosAsync(int id)
        {
            return Task.FromResult(false);
        }
    }
}

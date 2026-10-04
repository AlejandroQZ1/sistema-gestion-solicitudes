using AdministracionSoluciones.Models;
using Microsoft.EntityFrameworkCore;

namespace AdministracionSoluciones.Data
{
    /// <summary>
    /// Conexión con la base de datos. Los demás integrantes pueden agregar
    /// aquí sus propias tablas (Solicitudes, Tareas, etc.) como DbSet.
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Bitacora> Bitacora => Set<Bitacora>();
    }
}

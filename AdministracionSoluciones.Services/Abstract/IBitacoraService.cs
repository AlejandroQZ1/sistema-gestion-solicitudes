namespace AdministracionSoluciones.Services.Abstract
{
    public interface IBitacoraService
    {
        /// <summary>Guarda un registro en la bitácora. Nunca pasar contraseñas dentro de "datos".</summary>
        Task RegistrarAsync(string usuario, string modulo, string accion, object? datos = null);
    }
}

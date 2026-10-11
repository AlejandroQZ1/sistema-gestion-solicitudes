namespace AdministracionSoluciones.Services
{
    /// <summary>
    /// Respuesta de una operación de negocio: si salió bien y el mensaje para mostrar.
    /// "Campo" indica qué campo del formulario tiene el error (si aplica).
    /// </summary>
    public class Resultado
    {
        public bool Exito { get; private set; }
        public string Mensaje { get; private set; } = string.Empty;
        public string? Campo { get; private set; }

        public static Resultado Ok(string mensaje) => new() { Exito = true, Mensaje = mensaje };

        public static Resultado Error(string mensaje, string? campo = null) => new() { Exito = false, Mensaje = mensaje, Campo = campo };
    }
}

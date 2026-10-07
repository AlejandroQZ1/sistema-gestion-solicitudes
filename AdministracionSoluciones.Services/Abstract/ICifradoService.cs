namespace AdministracionSoluciones.Services.Abstract
{
    public interface ICifradoService
    {
        string Cifrar(string textoPlano);
        string Descifrar(string textoCifrado);
        bool ContrasenaEsCorrecta(string contrasenaEscrita, string contrasenaGuardada);
    }
}

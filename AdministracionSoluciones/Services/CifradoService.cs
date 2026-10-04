using System.Security.Cryptography;
using System.Text;

namespace AdministracionSoluciones.Services
{
    /// <summary>
    /// Cifrado de contraseñas con AES-GCM de 256 bits (USR4).
    /// El texto cifrado se guarda en Base64 con el formato: nonce (12 bytes) + tag (16 bytes) + datos.
    /// La clave (32 bytes en Base64) se lee de appsettings.json, sección "Cifrado:Clave".
    /// Todo el equipo debe usar la MISMA clave, o no se podrán leer las contraseñas guardadas.
    /// </summary>
    public class CifradoService
    {
        private const int TamanoNonce = 12;
        private const int TamanoTag = 16;
        private readonly byte[] _clave;

        public CifradoService(IConfiguration configuration)
        {
            var claveBase64 = configuration["Cifrado:Clave"]
                ?? throw new InvalidOperationException("Falta la configuración 'Cifrado:Clave' en appsettings.json.");

            _clave = Convert.FromBase64String(claveBase64);
            if (_clave.Length != 32)
            {
                throw new InvalidOperationException("La clave 'Cifrado:Clave' debe ser de 32 bytes (256 bits) en Base64.");
            }
        }

        public string Cifrar(string textoPlano)
        {
            var datos = Encoding.UTF8.GetBytes(textoPlano);
            var nonce = RandomNumberGenerator.GetBytes(TamanoNonce);
            var cifrado = new byte[datos.Length];
            var tag = new byte[TamanoTag];

            using var aes = new AesGcm(_clave, TamanoTag);
            aes.Encrypt(nonce, datos, cifrado, tag);

            var resultado = new byte[TamanoNonce + TamanoTag + cifrado.Length];
            Buffer.BlockCopy(nonce, 0, resultado, 0, TamanoNonce);
            Buffer.BlockCopy(tag, 0, resultado, TamanoNonce, TamanoTag);
            Buffer.BlockCopy(cifrado, 0, resultado, TamanoNonce + TamanoTag, cifrado.Length);

            return Convert.ToBase64String(resultado);
        }

        public string Descifrar(string textoCifrado)
        {
            var bytes = Convert.FromBase64String(textoCifrado);
            var nonce = bytes.AsSpan(0, TamanoNonce);
            var tag = bytes.AsSpan(TamanoNonce, TamanoTag);
            var cifrado = bytes.AsSpan(TamanoNonce + TamanoTag);
            var datos = new byte[cifrado.Length];

            using var aes = new AesGcm(_clave, TamanoTag);
            aes.Decrypt(nonce, cifrado, tag, datos);

            return Encoding.UTF8.GetString(datos);
        }

        /// <summary>Compara la contraseña escrita con la guardada (cifrada).</summary>
        public bool ContrasenaEsCorrecta(string contrasenaEscrita, string contrasenaGuardada)
        {
            try
            {
                var real = Encoding.UTF8.GetBytes(Descifrar(contrasenaGuardada));
                var escrita = Encoding.UTF8.GetBytes(contrasenaEscrita);
                return CryptographicOperations.FixedTimeEquals(real, escrita);
            }
            catch (Exception ex) when (ex is FormatException or CryptographicException or ArgumentException)
            {
                // Dato dañado o cifrado con otra clave
                return false;
            }
        }
    }
}

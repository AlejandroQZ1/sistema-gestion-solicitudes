using AdministracionSoluciones.Services.Abstract;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace AdministracionSoluciones.Services;

public class CorreoService : ICorreoService
{
    private readonly IConfiguration _configuracion;
    private readonly ILogger<CorreoService> _logger;

    public CorreoService(
        IConfiguration configuracion,
        ILogger<CorreoService> logger)
    {
        _configuracion = configuracion;
        _logger = logger;
    }

    public async Task<bool> EnviarCambioEstadoAsync(
        string destinatario,
        string consecutivoOficio,
        string tituloSolicitud,
        string estadoAnterior,
        string estadoNuevo,
        string usuarioActual)
    {
        if (string.IsNullOrWhiteSpace(destinatario))
        {
            _logger.LogWarning(
                "No se envió el correo de cambio de estado: la solicitud {Consecutivo} no tiene correo de representante.",
                consecutivoOficio);

            return false;
        }

        var servidor = _configuracion["Correo:SmtpServidor"];
        var puertoTexto = _configuracion["Correo:SmtpPuerto"];
        var usuario = _configuracion["Correo:Usuario"];
        var contrasena = _configuracion["Correo:Contrasena"];
        var remitente = _configuracion["Correo:Remitente"];

        if (string.IsNullOrWhiteSpace(servidor) ||
            !int.TryParse(puertoTexto, out var puerto) ||
            puerto <= 0 ||
            string.IsNullOrWhiteSpace(usuario) ||
            string.IsNullOrWhiteSpace(contrasena) ||
            string.IsNullOrWhiteSpace(remitente))
        {
            _logger.LogWarning(
                "El servicio de correo no está configurado correctamente.");

            return false;
        }

        try
        {
            var mensaje = new MimeMessage();

            mensaje.From.Add(
                new MailboxAddress(
                    "Administración de solicitudes",
                    remitente));

            mensaje.To.Add(
                MailboxAddress.Parse(destinatario));

            mensaje.Subject =
                $"Cambio de estado de solicitud: {consecutivoOficio}";

            var cuerpo = new BodyBuilder
            {
                TextBody =
                    $"""
                    Se ha actualizado el estado de una solicitud.

                    Consecutivo de oficio: {consecutivoOficio}
                    Título: {tituloSolicitud}
                    Estado anterior: {estadoAnterior}
                    Estado nuevo: {estadoNuevo}
                    Usuario responsable del cambio: {usuarioActual}

                    Este mensaje fue generado automáticamente por el
                    Sistema de Gestión de Solicitudes.
                    """
            };

            mensaje.Body = cuerpo.ToMessageBody();

            using var cliente = new SmtpClient();

            await cliente.ConnectAsync(
                servidor,
                puerto,
                SecureSocketOptions.StartTls);

            await cliente.AuthenticateAsync(
                usuario,
                contrasena);

            await cliente.SendAsync(mensaje);

            await cliente.DisconnectAsync(true);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Falló el envío del correo para la solicitud {Consecutivo}.",
                consecutivoOficio);

            return false;
        }
    }
}
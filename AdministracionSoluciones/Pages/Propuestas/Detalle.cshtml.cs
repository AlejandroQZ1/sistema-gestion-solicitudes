using AdministracionSoluciones.Entities.Propuestas;
using AdministracionSoluciones.Seguridad;
using AdministracionSoluciones.Services.Abstract;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AdministracionSoluciones.Pages.Propuestas;

[ValidateAntiForgeryToken]
public sealed class DetalleModel : PageModel
{
    private readonly IPropuestaService _propuestas;
    private readonly IBitacoraService _bitacora;

    public DetalleModel(IPropuestaService propuestas, IBitacoraService bitacora)
    {
        _propuestas = propuestas;
        _bitacora = bitacora;
    }

    [BindProperty(SupportsGet = true)]
    public int SolicitudId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PropuestaId { get; set; }

    public PropuestaDetalle Propuesta { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync()
    {
        var usuario = Sesion.NombreUsuario(HttpContext);
        try
        {
            if (!await CargarAsync()) return NotFound();
            await _bitacora.RegistrarAsync(usuario, "Propuestas", "El usuario consulta detalle de propuesta");
            return Page();
        }
        catch (Exception error)
        {
            await _bitacora.RegistrarAsync(usuario, "Propuestas", "Error al consultar propuesta",
                new { SolicitudId, PropuestaId, Error = error.ToString() });
            TempData["Error"] = "No se pudo consultar la propuesta.";
            return RedirectToPage("Index", new { solicitudId = SolicitudId });
        }
    }

    public async Task<IActionResult> OnPostAprobarAsync()
    {
        // El actor proviene únicamente de la sesión creada al autenticar SIGES.
        var actor = Sesion.NombreUsuario(HttpContext);
        try
        {
            var resultado = await _propuestas.AprobarAsync(SolicitudId, PropuestaId, actor);
            TempData[resultado.Exito ? "Exito" : "Error"] = resultado.Mensaje;
        }
        catch (Exception error)
        {
            await _bitacora.RegistrarAsync(actor, "Propuestas", "Error al aprobar propuesta",
                new { SolicitudId, PropuestaId, Error = error.ToString() });
            TempData["Error"] = "No se pudo aprobar la propuesta.";
        }
        return RedirectToPage("Detalle", new { solicitudId = SolicitudId, propuestaId = PropuestaId });
    }

    private async Task<bool> CargarAsync()
    {
        var detalle = await _propuestas.ObtenerDetalleAsync(SolicitudId, PropuestaId);
        if (detalle == null) return false;
        Propuesta = detalle;
        return true;
    }
}

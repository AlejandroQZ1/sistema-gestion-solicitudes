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

    public DetalleModel(IPropuestaService propuestas) => _propuestas = propuestas;

    [BindProperty(SupportsGet = true)]
    public int SolicitudId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PropuestaId { get; set; }

    public PropuestaDetalle Propuesta { get; private set; } = null!;
    public AprobacionPropuesta? Aprobacion { get; private set; }
    public IReadOnlyList<DesglosePropuestaHistorial> Historial { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await CargarAsync()) return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostAprobarAsync()
    {
        // El actor proviene únicamente de la sesión creada al autenticar SIGES.
        var actor = Sesion.NombreUsuario(HttpContext);
        var resultado = await _propuestas.AprobarAsync(SolicitudId, PropuestaId, actor);
        TempData[resultado.Exito ? "Exito" : "Error"] = resultado.Mensaje;
        return RedirectToPage("Detalle", new { solicitudId = SolicitudId, propuestaId = PropuestaId });
    }

    private async Task<bool> CargarAsync()
    {
        var detalle = await _propuestas.ObtenerDetalleAsync(SolicitudId, PropuestaId);
        if (detalle == null) return false;
        Propuesta = detalle;
        Aprobacion = await _propuestas.ObtenerAprobacionAsync(SolicitudId);
        if (Aprobacion != null)
            Historial = await _propuestas.ListarHistorialAsync(SolicitudId);
        return true;
    }
}

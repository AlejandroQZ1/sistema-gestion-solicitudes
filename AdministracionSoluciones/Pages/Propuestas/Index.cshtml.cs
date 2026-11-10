using AdministracionSoluciones.Entities.Propuestas;
using AdministracionSoluciones.Services.Abstract;
using AdministracionSoluciones.Seguridad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AdministracionSoluciones.Pages.Propuestas;

public sealed class IndexModel : PageModel
{
    private readonly IPropuestaService _propuestas;
    private readonly IBitacoraService _bitacora;

    public IndexModel(IPropuestaService propuestas, IBitacoraService bitacora)
    {
        _propuestas = propuestas;
        _bitacora = bitacora;
    }

    [BindProperty(SupportsGet = true)]
    public int SolicitudId { get; set; }

    public IReadOnlyList<PropuestaResumen> Propuestas { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        if (SolicitudId <= 0) return NotFound();
        var usuario = Sesion.NombreUsuario(HttpContext);
        try
        {
            Propuestas = await _propuestas.ListarPorSolicitudAsync(SolicitudId);
            await _bitacora.RegistrarAsync(usuario, "Propuestas", "El usuario consulta propuestas");
        }
        catch (Exception error)
        {
            await _bitacora.RegistrarAsync(usuario, "Propuestas", "Error al consultar propuestas",
                new { SolicitudId, Error = error.ToString() });
            TempData["Error"] = "No se pudieron consultar las propuestas.";
        }
        return Page();
    }
}

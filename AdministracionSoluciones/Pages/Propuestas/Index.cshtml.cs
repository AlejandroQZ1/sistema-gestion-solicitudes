using AdministracionSoluciones.Entities.Propuestas;
using AdministracionSoluciones.Services.Abstract;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AdministracionSoluciones.Pages.Propuestas;

public sealed class IndexModel : PageModel
{
    private readonly IPropuestaService _propuestas;

    public IndexModel(IPropuestaService propuestas) => _propuestas = propuestas;

    [BindProperty(SupportsGet = true)]
    public int SolicitudId { get; set; }

    public IReadOnlyList<PropuestaResumen> Propuestas { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        if (SolicitudId <= 0) return NotFound();
        Propuestas = await _propuestas.ListarPorSolicitudAsync(SolicitudId);
        return Page();
    }
}

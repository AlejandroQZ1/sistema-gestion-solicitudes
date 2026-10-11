
using AdministracionSoluciones.Entities;
using AdministracionSoluciones.Seguridad;
using AdministracionSoluciones.Services.Abstract;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AdministracionSoluciones.Pages.Solicitudes;

public class IndexModel : PageModel
{
    private const int RegistrosPorPagina = 5;

    private readonly ISolicitudService _solicitudService;

    public IndexModel(ISolicitudService solicitudService)
    {
        _solicitudService = solicitudService;
    }

    public IEnumerable<Solicitud> Solicitudes { get; private set; }
        = Enumerable.Empty<Solicitud>();

    public int Total { get; private set; }

    public int Pagina { get; private set; } = 1;

    public int TotalPaginas { get; private set; } = 1;

    public async Task OnGetAsync(int pagina = 1)
    {
        pagina = Math.Max(1, pagina);

        var resultado = await _solicitudService.ListarAsync(pagina);

        Solicitudes = resultado.Items;
        Total = resultado.Total;
        Pagina = resultado.Pagina;

        TotalPaginas = Math.Max(
            1,
            (int)Math.Ceiling(Total / (double)RegistrosPorPagina));
    }

    public async Task<IActionResult> OnPostEliminarAsync(
        int id,
        int pagina = 1)
    {
        pagina = Math.Max(1, pagina);

        var usuarioActual = Sesion.NombreUsuario(HttpContext);

        var resultado = await _solicitudService.EliminarAsync(
            id,
            usuarioActual);

        TempData[resultado.Exito ? "Exito" : "Error"] =
            resultado.Mensaje;

        return RedirectToPage(
            "/Solicitudes/Index",
            new { pagina });
    }
}

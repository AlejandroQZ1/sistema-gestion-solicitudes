
using AdministracionSoluciones.Entities;
using AdministracionSoluciones.Seguridad;
using AdministracionSoluciones.Services;
using AdministracionSoluciones.Services.Abstract;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AdministracionSoluciones.Pages.Solicitudes;

public class EditModel : PageModel
{
    private readonly ISolicitudService _solicitudService;
    private readonly CatalogoService _catalogoService;

    public EditModel(
        ISolicitudService solicitudService,
        CatalogoService catalogoService)
    {
        _solicitudService = solicitudService;
        _catalogoService = catalogoService;
    }

    [BindProperty]
    public Solicitud Solicitud { get; set; } = new();

    public List<CatalogoItem> Representantes { get; private set; } = [];

    public List<CatalogoItem> Estados { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (id <= 0)
        {
            return NotFound();
        }

        var solicitud = await _solicitudService.ObtenerAsync(id);
        if (solicitud == null)
        {
            return NotFound();
        }

        Solicitud = solicitud;
        await CargarCatalogosAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (id <= 0 || Solicitud.SolicitudID != id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await CargarCatalogosAsync();
            return Page();
        }

        var usuarioActual = Sesion.NombreUsuario(HttpContext);
        var resultado = await _solicitudService.ActualizarAsync(
            Solicitud,
            usuarioActual);

        if (!resultado.Exito)
        {
            ModelState.AddModelError(string.Empty, resultado.Mensaje);
            await CargarCatalogosAsync();
            return Page();
        }

        TempData["Exito"] = resultado.Mensaje;
        return RedirectToPage("/Solicitudes/Index");
    }

    private async Task CargarCatalogosAsync()
    {
        Representantes = await _catalogoService.ListarTodosAsync(
            TipoCatalogo.Representantes);
        Estados = await _catalogoService.ListarTodosAsync(
            TipoCatalogo.Estados);
    }
}

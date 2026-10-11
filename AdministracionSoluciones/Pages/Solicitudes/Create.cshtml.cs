
using AdministracionSoluciones.Entities;
using AdministracionSoluciones.Seguridad;
using AdministracionSoluciones.Services;
using AdministracionSoluciones.Services.Abstract;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AdministracionSoluciones.Pages.Solicitudes;

public class CreateModel : PageModel
{
    private readonly ISolicitudService _solicitudService;
    private readonly CatalogoService _catalogoService;

    public CreateModel(
        ISolicitudService solicitudService,
        CatalogoService catalogoService)
    {
        _solicitudService = solicitudService;
        _catalogoService = catalogoService;
    }

    [BindProperty]
    public Solicitud Solicitud { get; set; } = new();

    public List<CatalogoItem> Representantes { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Solicitud.FechaIngreso = DateTime.Today;

        await CargarRepresentantesAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // El servicio asigna automáticamente el estado NUEVA.
        ModelState.Remove("Solicitud.EstadoSolicitudID");

        if (!ModelState.IsValid)
        {
            await CargarRepresentantesAsync();
            return Page();
        }

        var usuarioActual = Sesion.NombreUsuario(HttpContext);

        var resultado = await _solicitudService.CrearAsync(
            Solicitud,
            usuarioActual);

        if (!resultado.Exito)
        {
            var campo = string.IsNullOrWhiteSpace(resultado.Campo)
                ? string.Empty
                : $"Solicitud.{resultado.Campo}";

            ModelState.AddModelError(campo, resultado.Mensaje);

            await CargarRepresentantesAsync();
            return Page();
        }

        TempData["Exito"] = resultado.Mensaje;

        return RedirectToPage("/Solicitudes/Index");
    }

    private async Task CargarRepresentantesAsync()
    {
        Representantes = await _catalogoService.ListarTodosAsync(
            TipoCatalogo.Representantes);
    }
}

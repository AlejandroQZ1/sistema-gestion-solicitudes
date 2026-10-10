using AdministracionSoluciones.Entities;
using AdministracionSoluciones.Services;
using AdministracionSoluciones.Seguridad;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace AdministracionSoluciones.Pages.Representantes;
public class IndexModel(CatalogoService service) : PageModel
{
    public List<CatalogoItem> Items { get; set; } = new();
    public int Pagina { get; set; }
    public int Paginas { get; set; }
    public int Total { get; set; }
    public string? Error { get; set; }
    public async Task OnGetAsync(int pagina = 1)
    {
        try {
            var datos = await service.ListarAsync(TipoCatalogo.Representantes, pagina, Sesion.NombreUsuario(HttpContext));
            Items = datos.Items; Total = datos.Total; Pagina = datos.Pagina;
            Paginas = Math.Max(1, (Total + 9) / 10);
        }
        catch { Error = "No se pudo cargar el listado. Revise la conexión y el script de catálogos."; }
    }
}

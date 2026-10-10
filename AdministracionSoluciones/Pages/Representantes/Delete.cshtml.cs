using AdministracionSoluciones.Entities;
using AdministracionSoluciones.Services;
using AdministracionSoluciones.Seguridad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace AdministracionSoluciones.Pages.Representantes;
public class DeleteModel(CatalogoService service) : PageModel
{
    public CatalogoItem? Item { get; set; }
    public async Task<IActionResult> OnGetAsync(int id) {
        try { Item = await service.ObtenerAsync(TipoCatalogo.Representantes, id); }
        catch (Exception ex) {
            await service.RegistrarErrorAsync(TipoCatalogo.Representantes, Sesion.NombreUsuario(HttpContext), ex);
            ModelState.AddModelError("", "No se pudo cargar el registro."); return Page();
        }
        return Item == null ? NotFound() : Page();
    }
    public async Task<IActionResult> OnPostAsync(int id) {
        if (id <= 0) return NotFound();
        var resultado = await service.GuardarAsync(TipoCatalogo.Representantes, new CatalogoItem { Id = id }, Sesion.NombreUsuario(HttpContext), eliminar: true);
        if (resultado.Exito) { TempData["Exito"] = resultado.Mensaje; return RedirectToPage("Index"); }
        await OnGetAsync(id);
        ModelState.AddModelError("", resultado.Mensaje);
        return Page();
    }
}

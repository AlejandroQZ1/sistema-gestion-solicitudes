using System.ComponentModel.DataAnnotations;
using AdministracionSoluciones.Entities;
using AdministracionSoluciones.Services;
using AdministracionSoluciones.Seguridad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace AdministracionSoluciones.Pages.EstadosSolicitudes;
public class FormModel(CatalogoService service) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public class InputModel {
        [Required(ErrorMessage = "El nombre es requerido.")]
        [StringLength(150)] public string Nombre { get; set; } = "";
        
        [Required(ErrorMessage = "Identificador es requerido.")]
        [StringLength(50)] public string Identificador { get; set; } = "";
    }
    public async Task<IActionResult> OnGetAsync(int id = 0) {
        if (id < 0) return NotFound();
        if (id == 0) return Page();
        try {
            var item = await service.ObtenerAsync(TipoCatalogo.Estados, id);
            if (item == null) return NotFound();
            Input = new() { Nombre = item.Nombre, Identificador = item.Identificador };
        } catch (Exception ex) {
            await service.RegistrarErrorAsync(TipoCatalogo.Estados, Sesion.NombreUsuario(HttpContext), ex);
            ModelState.AddModelError("", "No se pudo cargar el registro.");
        }
        return Page();
    }
    public async Task<IActionResult> OnPostAsync(int id = 0) {
        if (id < 0) return NotFound();
        if (!ModelState.IsValid) return Page();
        var resultado = await service.GuardarAsync(TipoCatalogo.Estados,
            new CatalogoItem { Id = id, Nombre = Input.Nombre, Identificador = Input.Identificador }, Sesion.NombreUsuario(HttpContext));
        if (!resultado.Exito) { ModelState.AddModelError("", resultado.Mensaje); return Page(); }
        TempData["Exito"] = resultado.Mensaje;
        return RedirectToPage("Index");
    }
}

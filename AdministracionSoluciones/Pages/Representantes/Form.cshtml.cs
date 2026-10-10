using System.ComponentModel.DataAnnotations;
using AdministracionSoluciones.Entities;
using AdministracionSoluciones.Services;
using AdministracionSoluciones.Seguridad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace AdministracionSoluciones.Pages.Representantes;
public class FormModel(CatalogoService service) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public class InputModel {
        [Required(ErrorMessage = "El nombre es requerido.")]
        [StringLength(150)] public string Nombre { get; set; } = "";
        [EmailAddress(ErrorMessage = "Ingrese un correo válido.")]
        [Required(ErrorMessage = "Correo electrónico es requerido.")]
        [StringLength(150)] public string Email { get; set; } = "";
    }
    public async Task<IActionResult> OnGetAsync(int id = 0) {
        if (id < 0) return NotFound();
        if (id == 0) return Page();
        try {
            var item = await service.ObtenerAsync(TipoCatalogo.Representantes, id);
            if (item == null) return NotFound();
            Input = new() { Nombre = item.Nombre, Email = item.Email };
        } catch (Exception ex) {
            await service.RegistrarErrorAsync(TipoCatalogo.Representantes, Sesion.NombreUsuario(HttpContext), ex);
            ModelState.AddModelError("", "No se pudo cargar el registro.");
        }
        return Page();
    }
    public async Task<IActionResult> OnPostAsync(int id = 0) {
        if (id < 0) return NotFound();
        if (!ModelState.IsValid) return Page();
        var resultado = await service.GuardarAsync(TipoCatalogo.Representantes,
            new CatalogoItem { Id = id, Nombre = Input.Nombre, Email = Input.Email }, Sesion.NombreUsuario(HttpContext));
        if (!resultado.Exito) { ModelState.AddModelError("", resultado.Mensaje); return Page(); }
        TempData["Exito"] = resultado.Mensaje;
        return RedirectToPage("Index");
    }
}

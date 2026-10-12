using AdministracionSoluciones.Seguridad;
using AdministracionSoluciones.Services.Abstract;
using Microsoft.AspNetCore.Mvc;

namespace AdministracionSoluciones.Pages.Tareas;

// TAR1: alta de una tarea con el usuario conectado seleccionado por defecto.
public class CreateModel : FormularioTareaModel
{
    public CreateModel(ITareaService tareas) : base(tareas)
    {
    }

    public async Task<IActionResult> OnGetAsync(int solicitudId)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }
        if (!await PrepararAsync(solicitudId))
        {
            return NotFound();
        }
        Input.UsuarioId = Sesion.UsuarioID(HttpContext);
        await tareas.ConsultaAsync(Sesion.NombreUsuario(HttpContext), "formulario de tareas");
        return Page();
    }

    public Task<IActionResult> OnPostAsync(int solicitudId)
    {
        return GuardarAsync(solicitudId, null);
    }
}

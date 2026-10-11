using AdministracionSoluciones.Seguridad;
using AdministracionSoluciones.Services.Abstract;
using Microsoft.AspNetCore.Mvc;

namespace AdministracionSoluciones.Pages.Tareas;

// TAR1: edicion de una tarea sin modificar su solicitud original.
public class EditModel : FormularioTareaModel
{
    public EditModel(ITareaService tareas) : base(tareas)
    {
    }

    public async Task<IActionResult> OnGetAsync(int id, int solicitudId)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }
        var tarea = await tareas.ObtenerAsync(id, solicitudId, Sesion.NombreUsuario(HttpContext));
        if (tarea == null || !await PrepararAsync(solicitudId))
        {
            return NotFound();
        }
        Input.TareaId = id;
        Input.Descripcion = tarea.Descripcion;
        Input.Fecha = tarea.Fecha;
        Input.UsuarioId = tarea.UsuarioId;
        Input.Horas = tarea.Horas;
        return Page();
    }

    public Task<IActionResult> OnPostAsync(int id, int solicitudId)
    {
        return GuardarAsync(solicitudId, id);
    }
}

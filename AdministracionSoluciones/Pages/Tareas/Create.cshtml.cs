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

    public async Task<IActionResult> OnGetAsync(int? solicitudId)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }
        ElegirSolicitud = !solicitudId.HasValue;
        if (ElegirSolicitud)
        {
            Todas = true;
            Solicitudes = await tareas.SolicitudesAsync();
            Input.Usuarios = await tareas.UsuariosAsync();
        }
        else if (!await PrepararAsync(solicitudId!.Value))
        {
            return NotFound();
        }
        Input.UsuarioId = Sesion.UsuarioID(HttpContext);
        await tareas.ConsultaAsync(Sesion.NombreUsuario(HttpContext), "formulario de tareas");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? solicitudId)
    {
        // La solicitud se elige únicamente cuando el alta se inicia en el listado general.
        ElegirSolicitud = !solicitudId.HasValue;
        if (ElegirSolicitud)
        {
            Todas = true;
            Solicitudes = await tareas.SolicitudesAsync();
            Input.Usuarios = await tareas.UsuariosAsync();
            if (!SolicitudSeleccionada.HasValue || !Solicitudes.Any(s => s.Id == SolicitudSeleccionada.Value))
            {
                ModelState.AddModelError("SolicitudSeleccionada", "Seleccione una solicitud existente.");
                return Page();
            }
            solicitudId = SolicitudSeleccionada.Value;
        }
        return await GuardarAsync(solicitudId!.Value, null);
    }
}

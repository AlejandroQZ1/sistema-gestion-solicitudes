using AdministracionSoluciones.Entities;
using AdministracionSoluciones.Infraestructura;
using AdministracionSoluciones.Models.Tareas;
using AdministracionSoluciones.Seguridad;
using AdministracionSoluciones.Services.Abstract;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;

namespace AdministracionSoluciones.Pages.Tareas;

// Comparte la carga y validacion de los formularios Crear y Editar.
[ServiceFilter(typeof(ErroresTareas))]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public abstract class FormularioTareaModel : PageModel
{
    protected readonly ITareaService tareas;
    [BindProperty]
    public TareaFormulario Input { get; set; } = new();

    // Mantiene el listado de origen al guardar o cancelar.
    [BindProperty(SupportsGet = true)]
    public bool Todas { get; set; }

    [BindProperty]
    public int? SolicitudSeleccionada { get; set; }
    public bool ElegirSolicitud { get; set; }
    public IReadOnlyList<SolicitudOpcion> Solicitudes { get; set; } = [];

    protected FormularioTareaModel(ITareaService tareas)
    {
        this.tareas = tareas;
    }

    // Los datos auxiliares siempre se obtienen del servidor, no del formulario enviado.
    protected async Task<bool> PrepararAsync(int solicitudId)
    {
        var solicitud = await tareas.SolicitudAsync(solicitudId);
        if (solicitud == null)
        {
            return false;
        }
        Input.Solicitud = solicitud;
        Input.Usuarios = await tareas.UsuariosAsync();
        return true;
    }

    // Solo se trasladan al servicio los cuatro campos editables definidos en TAR1.
    protected async Task<IActionResult> GuardarAsync(int solicitudId, int? id)
    {
        Input.TareaId = id;
        if (!await PrepararAsync(solicitudId))
        {
            return NotFound();
        }
        if (id.HasValue && await tareas.ObtenerAsync(id.Value, solicitudId, Sesion.NombreUsuario(HttpContext)) == null)
        {
            return NotFound();
        }
        var datos = new TareaDatos
        {
            Descripcion = Input.Descripcion,
            Fecha = Input.Fecha,
            UsuarioId = Input.UsuarioId,
            Horas = Input.Horas
        };
        // Los errores de campos obligatorios o de formato se resuelven antes de validar relaciones.
        if (ModelState.IsValid)
        {
            var error = await tareas.ValidarAsync(datos);
            if (error != null)
            {
                ModelState.AddModelError("", error);
            }
        }
        if (!ModelState.IsValid)
        {
            return Page();
        }
        try
        {
            if (id.HasValue)
            {
                if (!await tareas.EditarAsync(id.Value, solicitudId, datos, Sesion.NombreUsuario(HttpContext)))
                {
                    return NotFound();
                }
            }
            else
            {
                await tareas.CrearAsync(solicitudId, datos, Sesion.NombreUsuario(HttpContext));
            }
        }
        catch (MySqlException ex) when (ex.Number == 1452)
        {
            await tareas.RegistrarErrorAsync(Sesion.NombreUsuario(HttpContext), ex, HttpContext.TraceIdentifier);
            ModelState.AddModelError("", "El usuario o la solicitud ya no está disponible. Revise la selección.");
            return Page();
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return Page();
        }
        TempData["ExitoTareas"] = id.HasValue ? "La tarea se actualizó correctamente." : "La tarea se creó correctamente.";
        return Todas ? RedirectToPage("Index") : RedirectToPage("Index", new { solicitudId });
    }
}

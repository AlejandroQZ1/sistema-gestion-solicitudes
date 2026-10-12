using AdministracionSoluciones.Seguridad;
using AdministracionSoluciones.Services.Abstract;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AdministracionSoluciones.Infraestructura;

// Captura errores no controlados de las paginas de tareas y evita mostrar detalles internos.
public class ErroresTareas : IAsyncExceptionFilter
{
    private readonly ITareaService tareas;
    public ErroresTareas(ITareaService tareas)
    {
        this.tareas = tareas;
    }

    public async Task OnExceptionAsync(ExceptionContext context)
    {
        await tareas.RegistrarErrorAsync(Sesion.NombreUsuario(context.HttpContext), context.Exception, context.HttpContext.TraceIdentifier);
        // La pagina de error conserva la plantilla global y no consulta la base de datos.
        context.Result = new RedirectToPageResult("/Tareas/Error");
        context.ExceptionHandled = true;
    }
}

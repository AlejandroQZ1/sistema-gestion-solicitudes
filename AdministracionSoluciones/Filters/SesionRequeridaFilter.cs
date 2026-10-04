using AdministracionSoluciones.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace AdministracionSoluciones.Filters
{
    /// <summary>
    /// Marca una acción o controlador que se puede usar sin iniciar sesión (por ejemplo, el login).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class PermitirSinSesionAttribute : Attribute
    {
    }

    /// <summary>
    /// Filtro global (USR1): si alguien intenta entrar a cualquier pantalla sin haber
    /// iniciado sesión, lo envía al login con el mensaje correspondiente.
    /// Se registra en Program.cs, así que protege automáticamente todas las pantallas
    /// nuevas que creen los demás integrantes.
    /// </summary>
    public class SesionRequeridaFilter : IActionFilter
    {
        public const string MensajeSinSesion = "Por favor inicie sesión para utilizar el sistema";

        private readonly ITempDataDictionaryFactory _tempDataFactory;

        public SesionRequeridaFilter(ITempDataDictionaryFactory tempDataFactory)
        {
            _tempDataFactory = tempDataFactory;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var permitido = context.ActionDescriptor.EndpointMetadata.OfType<PermitirSinSesionAttribute>().Any();
            if (permitido || Sesion.EstaIniciada(context.HttpContext))
            {
                return;
            }

            var tempData = _tempDataFactory.GetTempData(context.HttpContext);
            tempData["MensajeLogin"] = MensajeSinSesion;
            context.Result = new RedirectToActionResult("Index", "Login", null);
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }
    }
}

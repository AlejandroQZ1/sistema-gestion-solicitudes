using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc;

namespace AdministracionSoluciones.Seguridad
{
    /// <summary>
    /// Se pone sobre una página (PageModel) que se puede usar sin iniciar sesión, por ejemplo el login.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class PermitirSinSesionAttribute : Attribute
    {
    }

    /// <summary>
    /// Filtro global (USR1): si alguien intenta entrar a cualquier página sin haber iniciado sesión,
    /// lo envía al login con el mensaje correspondiente. Se registra en Program.cs, así que protege
    /// automáticamente todas las páginas nuevas que creen los demás integrantes.
    /// </summary>
    public class SesionRequeridaFilter : IPageFilter
    {
        public const string MensajeSinSesion = "Por favor inicie sesión para utilizar el sistema";

        private readonly ITempDataDictionaryFactory _tempDataFactory;

        public SesionRequeridaFilter(ITempDataDictionaryFactory tempDataFactory)
        {
            _tempDataFactory = tempDataFactory;
        }

        public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
        {
            var permitida = context.HandlerInstance.GetType().IsDefined(typeof(PermitirSinSesionAttribute), true);
            if (permitida || Sesion.EstaIniciada(context.HttpContext))
            {
                return;
            }

            var tempData = _tempDataFactory.GetTempData(context.HttpContext);
            tempData["MensajeLogin"] = MensajeSinSesion;
            context.Result = new RedirectToPageResult("/Login/Index");
        }

        public void OnPageHandlerSelected(PageHandlerSelectedContext context)
        {
        }

        public void OnPageHandlerExecuted(PageHandlerExecutedContext context)
        {
        }
    }
}

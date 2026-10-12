using AdministracionSoluciones.Entities;
using AdministracionSoluciones.Seguridad;
using AdministracionSoluciones.Services.Abstract;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AdministracionSoluciones.Pages.Solicitudes
{
    /// <summary>
    /// SOL1 / SOL2 - Listado de Solicitudes con acceso al link "Desglose" según especificación.
    /// </summary>
    public class IndexModel : PageModel
    {
        private readonly IDesgloseService _desgloseService;

        public IndexModel(IDesgloseService desgloseService)
        {
            _desgloseService = desgloseService;
            Solicitudes = new List<Solicitud>();
        }

        public IEnumerable<Solicitud> Solicitudes { get; set; }

        public async Task OnGetAsync()
        {
            Solicitudes = await _desgloseService.GetSolicitudesParaComboAsync();
        }
    }
}

using AdministracionSoluciones.Entities;
using AdministracionSoluciones.Seguridad;
using AdministracionSoluciones.Services.Abstract;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AdministracionSoluciones.Pages.Desgloses
{
    /// <summary>
    /// SOL2 - Mantenimiento de Desgloses de Solicitudes.
    /// Soporta listado general o filtrado por solicitud, paginación a 10 ítems y eliminación con validación de asignación.
    /// </summary>
    public class IndexModel : PageModel
    {
        private const int TamanoPagina = 10;
        private readonly IDesgloseService _desgloseService;

        public IndexModel(IDesgloseService desgloseService)
        {
            _desgloseService = desgloseService;
            Desgloses = new List<Desglose>();
            SolicitudesDisponibles = new List<Solicitud>();
        }

        [BindProperty(SupportsGet = true)]
        public int? SolicitudId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int Pagina { get; set; } = 1;

        public IEnumerable<Desglose> Desgloses { get; set; }
        public IEnumerable<Solicitud> SolicitudesDisponibles { get; set; }
        public Solicitud? SolicitudSeleccionada { get; set; }

        public int TotalRegistros { get; set; }
        public int TotalPaginas { get; set; }
        public int PaginaActual => Pagina < 1 ? 1 : Pagina;

        public async Task OnGetAsync()
        {
            var usuario = Sesion.NombreUsuario(HttpContext);
            var todos = (await _desgloseService.GetAllAsync(SolicitudId, usuario)).ToList();

            SolicitudesDisponibles = await _desgloseService.GetSolicitudesParaComboAsync();

            if (SolicitudId.HasValue)
            {
                SolicitudSeleccionada = await _desgloseService.GetSolicitudByIdAsync(SolicitudId.Value);
            }

            TotalRegistros = todos.Count;
            TotalPaginas = (int)Math.Ceiling(TotalRegistros / (double)TamanoPagina);
            if (TotalPaginas < 1) TotalPaginas = 1;

            if (Pagina > TotalPaginas) Pagina = TotalPaginas;

            Desgloses = todos
                .Skip((PaginaActual - 1) * TamanoPagina)
                .Take(TamanoPagina)
                .ToList();
        }

        public async Task<IActionResult> OnPostEliminarAsync(int id)
        {
            var usuario = Sesion.NombreUsuario(HttpContext);
            var resultado = await _desgloseService.EliminarAsync(id, usuario);

            if (resultado.Exito)
            {
                TempData["Exito"] = resultado.Mensaje;
            }
            else
            {
                TempData["Error"] = resultado.Mensaje;
            }

            return RedirectToPage("Index", new { solicitudId = SolicitudId, pagina = Pagina });
        }
    }
}

using AdministracionSoluciones.Seguridad;
using AdministracionSoluciones.Services.Abstract;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AdministracionSoluciones.Pages.Reportes
{
    /// <summary>
    /// SOL7 - Reporte ejecutivo de montos que se facturarán por mes entre todas las solicitudes.
    /// Recibe mes y año como parámetros, calcula totales y registra la consulta en bitácora.
    /// </summary>
    public class MontosFacturacionModel : PageModel
    {
        private readonly IReporteService _reporteService;

        public MontosFacturacionModel(IReporteService reporteService)
        {
            _reporteService = reporteService;
        }

        [BindProperty(SupportsGet = true)]
        public int Mes { get; set; } = DateTime.Now.Month;

        [BindProperty(SupportsGet = true)]
        public int Anio { get; set; } = DateTime.Now.Year;

        public ReporteMontosFacturacionResult Reporte { get; set; } = null!;
        public SelectList MesesSelectList { get; set; } = null!;

        public async Task OnGetAsync()
        {
            // Ajustar valores por defecto si vienen fuera de rango
            if (Mes < 1 || Mes > 12) Mes = DateTime.Now.Month;
            if (Anio < 2000 || Anio > 2100) Anio = DateTime.Now.Year;

            CargarListaMeses();

            var usuario = Sesion.NombreUsuario(HttpContext);
            Reporte = await _reporteService.ObtenerReporteMontosFacturacionAsync(Mes, Anio, usuario);
        }

        private void CargarListaMeses()
        {
            var meses = Enumerable.Range(1, 12).Select(m => new
            {
                Numero = m,
                Nombre = new DateTime(2026, m, 1).ToString("MMMM", new System.Globalization.CultureInfo("es-ES"))
            }).Select(m => new { m.Numero, Nombre = char.ToUpper(m.Nombre[0]) + m.Nombre.Substring(1) });

            MesesSelectList = new SelectList(meses, "Numero", "Nombre", Mes);
        }
    }
}

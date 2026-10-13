using AdministracionSoluciones.Entities;

namespace AdministracionSoluciones.Services.Abstract
{
    public class ReporteMontosFacturacionResult
    {
        public int Mes { get; set; }
        public int Anio { get; set; }
        public string NombreMes { get; set; } = string.Empty;
        public IEnumerable<ReporteMontoFacturacionItem> Items { get; set; } = new List<ReporteMontoFacturacionItem>();
        public decimal TotalMonto { get; set; }
        public decimal TotalIVA { get; set; }
        public decimal TotalGeneral { get; set; }
    }

    public interface IReporteService
    {
        Task<ReporteMontosFacturacionResult> ObtenerReporteMontosFacturacionAsync(int mes, int anio, string usuarioActual);
    }
}

using AdministracionSoluciones.Entities;
using AdministracionSoluciones.Repository;
using AdministracionSoluciones.Services.Abstract;

namespace AdministracionSoluciones.Services
{
    /// <summary>
    /// SOL7 - Reglas y generación de reporte de montos que se facturarán por mes entre todas las solicitudes.
    /// Registra en bitácora y calcula sumatorias generales.
    /// </summary>
    public class ReporteService : IReporteService
    {
        private const string Modulo = "Reportes";
        private readonly ReporteRepository _reporteRepository;
        private readonly IBitacoraService _bitacoraService;

        public ReporteService(ReporteRepository reporteRepository, IBitacoraService bitacoraService)
        {
            _reporteRepository = reporteRepository;
            _bitacoraService = bitacoraService;
        }

        public async Task<ReporteMontosFacturacionResult> ObtenerReporteMontosFacturacionAsync(int mes, int anio, string usuarioActual)
        {
            try
            {
                var items = (await _reporteRepository.ObtenerReporteMontosFacturacionAsync(mes, anio)).ToList();

                var totalMonto = items.Sum(i => i.Monto);
                var totalIVA = items.Sum(i => i.IVA);
                var totalGeneral = items.Sum(i => i.Total);

                string nombreMes = mes switch
                {
                    1 => "Enero",
                    2 => "Febrero",
                    3 => "Marzo",
                    4 => "Abril",
                    5 => "Mayo",
                    6 => "Junio",
                    7 => "Julio",
                    8 => "Agosto",
                    9 => "Septiembre",
                    10 => "Octubre",
                    11 => "Noviembre",
                    12 => "Diciembre",
                    _ => $"Mes {mes}"
                };

                // Bitácora de consulta requerida
                if (!string.IsNullOrWhiteSpace(usuarioActual))
                {
                    await _bitacoraService.RegistrarAsync(
                        usuarioActual,
                        Modulo,
                        "Consultar",
                        $"El usuario consulta el reporte de montos a facturar para {nombreMes} de {anio} ({items.Count} registros)."
                    );
                }

                return new ReporteMontosFacturacionResult
                {
                    Mes = mes,
                    Anio = anio,
                    NombreMes = nombreMes,
                    Items = items,
                    TotalMonto = totalMonto,
                    TotalIVA = totalIVA,
                    TotalGeneral = totalGeneral
                };
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrWhiteSpace(usuarioActual))
                {
                    await _bitacoraService.RegistrarAsync(usuarioActual, Modulo, "Error Técnico", new { Reporte = "MontosFacturacion", Error = ex.Message });
                }
                throw;
            }
        }
    }
}

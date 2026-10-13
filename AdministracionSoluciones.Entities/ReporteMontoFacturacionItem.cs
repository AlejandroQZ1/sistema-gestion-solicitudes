using System.ComponentModel.DataAnnotations;

namespace AdministracionSoluciones.Entities
{
    /// <summary>
    /// Ítem de datos para el reporte de montos que se facturarán por mes entre todas las solicitudes (SOL7).
    /// </summary>
    public class ReporteMontoFacturacionItem
    {
        [Display(Name = "Consecutivo de oficio")]
        public string ConsecutivoOficio { get; set; } = string.Empty;

        [Display(Name = "Título de la solicitud")]
        public string Titulo { get; set; } = string.Empty;

        [Display(Name = "Mes")]
        public int Mes { get; set; }

        [Display(Name = "Año")]
        public int Anio { get; set; }

        [Display(Name = "Monto")]
        [DataType(DataType.Currency)]
        public decimal Monto { get; set; }

        [Display(Name = "IVA (13%)")]
        [DataType(DataType.Currency)]
        public decimal IVA { get; set; }

        [Display(Name = "Total por solicitud")]
        [DataType(DataType.Currency)]
        public decimal Total { get; set; }

        [Display(Name = "% Cobro")]
        public decimal PorcentajeCobro { get; set; }

        public string NombreMes => Mes switch
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
            _ => $"Mes {Mes}"
        };
    }
}

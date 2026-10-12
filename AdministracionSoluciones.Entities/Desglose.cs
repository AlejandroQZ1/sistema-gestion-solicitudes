using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace AdministracionSoluciones.Entities
{
    /// <summary>
    /// Entidad para la administración de desgloses de una solicitud (SOL2).
    /// Representa el trabajo y costo planificado mes a mes para una solicitud.
    /// </summary>
    public class Desglose
    {
        public int DesgloseID { get; set; }

        [Required(ErrorMessage = "La solicitud es requerida.")]
        [Display(Name = "Solicitud")]
        public int SolicitudID { get; set; }

        [Required(ErrorMessage = "El mes es requerido.")]
        [Range(1, 12, ErrorMessage = "El mes debe estar entre 1 y 12.")]
        [Display(Name = "Mes")]
        public int Mes { get; set; }

        [Required(ErrorMessage = "El año es requerido.")]
        [Range(2000, 2100, ErrorMessage = "El año debe ser válido (ej. 2026).")]
        [Display(Name = "Año")]
        public int Anio { get; set; }

        [Required(ErrorMessage = "Las horas son requeridas.")]
        [Range(0, 10000, ErrorMessage = "Las horas deben ser mayores o iguales a 0.")]
        [Display(Name = "Horas")]
        public decimal Horas { get; set; }

        [Required(ErrorMessage = "El monto es requerido.")]
        [Range(0, 999999999999, ErrorMessage = "El monto debe ser un valor positivo.")]
        [DataType(DataType.Currency)]
        [Display(Name = "Monto")]
        public decimal Monto { get; set; }

        [Display(Name = "IVA (13%)")]
        [DataType(DataType.Currency)]
        public decimal IVA { get; set; }

        [Display(Name = "Total (Monto + IVA)")]
        [DataType(DataType.Currency)]
        public decimal Total { get; set; }

        [Display(Name = "Observaciones")]
        [StringLength(1000, ErrorMessage = "Las observaciones no pueden exceder 1000 caracteres.")]
        public string? Observaciones { get; set; }

        [Required(ErrorMessage = "El porcentaje de cobro es requerido.")]
        [Range(0, 100, ErrorMessage = "El porcentaje de cobro debe estar entre 0% y 100%.")]
        [Display(Name = "Porcentaje de cobro (%)")]
        public decimal PorcentajeCobro { get; set; } = 100m;

        // Propiedades de navegación / lectura para tablas y reportes
        public string? ConsecutivoOficio { get; set; }
        public string? TituloSolicitud { get; set; }

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

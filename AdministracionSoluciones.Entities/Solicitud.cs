using System.ComponentModel.DataAnnotations;

namespace AdministracionSoluciones.Entities
{
    /// <summary>
    /// Entidad para la administración de solicitudes (SOL1).
    /// Representa un trabajo que se desea que realice algún proveedor.
    /// </summary>
    public class Solicitud
    {
        public int SolicitudID { get; set; }

        [Required(ErrorMessage = "El consecutivo de oficio es requerido.")]
        [StringLength(50)]
        [Display(Name = "Consecutivo de oficio")]
        public string ConsecutivoOficio { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Documento de respuesta")]
        public string? DocumentoRespuesta { get; set; }

        [StringLength(100)]
        [Display(Name = "Documento de inicio")]
        public string? DocumentoInicio { get; set; }

        [Required(ErrorMessage = "El título de la solicitud es requerido.")]
        [StringLength(200)]
        [Display(Name = "Título de la solicitud")]
        public string Titulo { get; set; } = string.Empty;

        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        [Display(Name = "Representante")]
        public int? RepresentanteID { get; set; }

        [Display(Name = "Observaciones")]
        public string? Observaciones { get; set; }

        public int EstadoSolicitudID { get; set; }

        // Nombre del estado obtenido del catálogo.
        [Display(Name = "Estado")]
        public string Estado { get; set; } = "Nueva";

        [Required(ErrorMessage = "La fecha de ingreso es requerida.")]
        [Display(Name = "Fecha de ingreso")]
        public DateTime FechaIngreso { get; set; } = DateTime.Now;

        [Display(Name = "Fecha de respuesta")]
        public DateTime? FechaRespuesta { get; set; }

        [Display(Name = "Fecha de inicio")]
        public DateTime? FechaInicio { get; set; }
    }
}

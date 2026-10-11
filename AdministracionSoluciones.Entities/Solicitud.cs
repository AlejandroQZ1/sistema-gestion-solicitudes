
namespace AdministracionSoluciones.Entities
{
    public class Solicitud
    {
        public int SolicitudID { get; set; }

        public string ConsecutivoOficio { get; set; } = string.Empty;

        public string? DocumentoRespuesta { get; set; }

        public string? DocumentoInicio { get; set; }

        public string Titulo { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        public int? RepresentanteID { get; set; }

        public string? Observaciones { get; set; }

        public int EstadoSolicitudID { get; set; }

        public DateTime FechaIngreso { get; set; }

        public DateTime? FechaRespuesta { get; set; }

        public DateTime? FechaInicio { get; set; }

        // Propiedades auxiliares para las consultas con Dapper.
        public string? NombreRepresentante { get; set; }

        public string? NombreEstado { get; set; }

        public string? IdentificadorEstado { get; set; }

        // Indica si la solicitud cumple hoy cinco días hábiles sin respuesta.
        public bool VenceHoy { get; set; }
        public string? EmailRepresentante { get; set; }
    }
}

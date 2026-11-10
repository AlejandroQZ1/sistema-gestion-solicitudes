namespace AdministracionSoluciones.Entities.Propuestas;

/// <summary>Snapshot de una fila formal sustituida por una aprobación EXT7.</summary>
public class DesgloseAnterior
{
    public int DesgloseID { get; set; }
    public int SolicitudID { get; set; }
    public int? DesglosePropuestaID { get; set; }
    public int? PropuestaID { get; set; }
    public int? Mes { get; set; }
    public int? Anio { get; set; }
    public decimal? Horas { get; set; }
    public decimal? Monto { get; set; }
    public decimal? IVA { get; set; }
    public decimal? Total { get; set; }
    public string? Observaciones { get; set; }
    public decimal? PorcentajeCobro { get; set; }
}

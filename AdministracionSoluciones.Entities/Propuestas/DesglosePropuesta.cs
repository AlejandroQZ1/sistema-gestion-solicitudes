namespace AdministracionSoluciones.Entities.Propuestas;

/// <summary>Línea almacenada en desgloses_propuesta.</summary>
public sealed class DesglosePropuesta
{
    public int DesglosePropuestaID { get; set; }
    public int PropuestaID { get; set; }
    public int Mes { get; set; }
    public int Anio { get; set; }
    public decimal Horas { get; set; }
    public decimal Monto { get; set; }
    public decimal IVA { get; set; }
    public decimal Total { get; set; }
    public string Observaciones { get; set; } = string.Empty;
    public decimal PorcentajeCobro { get; set; }
}

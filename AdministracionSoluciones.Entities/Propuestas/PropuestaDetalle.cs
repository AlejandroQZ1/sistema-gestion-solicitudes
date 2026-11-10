namespace AdministracionSoluciones.Entities.Propuestas;

/// <summary>Detalle de una propuesta y sus líneas.</summary>
public sealed class PropuestaDetalle : PropuestaResumen
{
    public string ConsecutivoOficio { get; set; } = string.Empty;
    public string TituloSolicitud { get; set; } = string.Empty;
    public IReadOnlyList<DesglosePropuesta> Lineas { get; set; } = [];
}

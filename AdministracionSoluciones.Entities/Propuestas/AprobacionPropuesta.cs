namespace AdministracionSoluciones.Entities.Propuestas;

/// <summary>Datos de auditoría persistidos para la aprobación de una propuesta.</summary>
public sealed class AprobacionPropuesta
{
    public int AprobacionID { get; set; }
    public int SolicitudID { get; set; }
    public int PropuestaID { get; set; }
    public int ProveedorID { get; set; }
    public string CedulaJuridica { get; set; } = string.Empty;
    public string NombreProveedor { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public DateTime FechaAprobacion { get; set; }
    public string DatosAnterioresJson { get; set; } = "[]";
    public string DatosNuevosJson { get; set; } = "[]";
}

/// <summary>Registro de desglose histórico archivado durante la aprobación.</summary>
public sealed class DesglosePropuestaHistorial : DesgloseAnterior
{
    public int HistorialDesgloseID { get; set; }
    public int AprobacionID { get; set; }
}

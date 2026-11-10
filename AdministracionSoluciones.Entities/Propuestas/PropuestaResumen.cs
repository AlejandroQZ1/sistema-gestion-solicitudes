namespace AdministracionSoluciones.Entities.Propuestas;

/// <summary>Datos de una propuesta y el proveedor que la registró.</summary>
public class PropuestaResumen
{
    public int PropuestaID { get; set; }
    public int SolicitudID { get; set; }
    public int ProveedorID { get; set; }
    public string CedulaJuridica { get; set; } = string.Empty;
    public string NombreProveedor { get; set; } = string.Empty;
    public DateTime FechaPropuesta { get; set; }
    public bool Aprobada { get; set; }
    public bool SolicitudTieneAprobacion { get; set; }
}

namespace AdministracionSoluciones.Entities.Tareas;

// Resultados y total de horas del reporte seleccionado.
public class ReporteTareas
{
    public FiltroReporte Filtro { get; set; } = new();
    public bool PorUsuario { get; set; }
    public IReadOnlyList<UsuarioOpcion> Usuarios { get; set; } = [];
    public IReadOnlyList<SolicitudOpcion> Solicitudes { get; set; } = [];
    public IReadOnlyList<TareaFila>? Filas { get; set; }
    // Las tareas sin horas registradas no aportan al total.
    public decimal TotalHoras
    {
        get
        {
            decimal total = 0;
            if (Filas != null)
            {
                foreach (var tarea in Filas)
                {
                    total += tarea.Horas ?? 0;
                }
            }
            return total;
        }
    }
    public string Seleccion { get; set; } = "";
}

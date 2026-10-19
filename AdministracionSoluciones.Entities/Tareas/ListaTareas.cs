namespace AdministracionSoluciones.Entities.Tareas;

// Información del listado de tareas y su paginación.
public class ListaTareas
{
    public SolicitudOpcion? Solicitud { get; set; }
    public IReadOnlyList<TareaFila> Filas { get; set; } = [];
    public int Pagina { get; set; } = 1;
    public long Cantidad { get; set; }
    // Mantiene al menos una página aunque la solicitud no tenga tareas.
    public long Paginas => Math.Max(1, (Cantidad + 9) / 10);
}

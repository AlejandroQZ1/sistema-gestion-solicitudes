namespace AdministracionSoluciones.Entities;

// Datos de usuario utilizados en los selectores y etiquetas.
public class UsuarioOpcion
{
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = "";
    public string NombreCompleto { get; set; } = "";
    public string Etiqueta => $"{NombreCompleto} ({NombreUsuario})";
}

// Identificación de la solicitud utilizada en formularios y listados.
public class SolicitudOpcion
{
    public int Id { get; set; }
    public string Consecutivo { get; set; } = "";
    public string Titulo { get; set; } = "";
    public string Etiqueta => $"{Consecutivo} — {Titulo}";
}

// Datos de una tarea y sus relaciones para los listados y reportes.
public class TareaFila
{
    public int Id { get; set; }
    public int SolicitudId { get; set; }
    public string? Descripcion { get; set; }
    public DateTime? Fecha { get; set; }
    public int UsuarioId { get; set; }
    public decimal? Horas { get; set; }
    public string NombreUsuario { get; set; } = "";
    public string NombreCompleto { get; set; } = "";
    public string Consecutivo { get; set; } = "";
    public string Titulo { get; set; } = "";
    // La bitácora conserva los campos propios de la tarea, sin datos de otras entidades.
    public object DatosBitacora()
    {
        return new { Id, SolicitudId, Descripcion, Fecha, UsuarioId, Horas };
    }
}


// Valores persistidos al crear o actualizar una tarea.
public class TareaDatos
{
    public string? Descripcion { get; set; }
    public DateTime? Fecha { get; set; }
    public int? UsuarioId { get; set; }
    public decimal? Horas { get; set; }
}

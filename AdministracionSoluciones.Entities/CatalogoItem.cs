namespace AdministracionSoluciones.Entities;

public enum TipoCatalogo { Representantes, Estados }

public class CatalogoItem
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Identificador { get; set; } = string.Empty;
}

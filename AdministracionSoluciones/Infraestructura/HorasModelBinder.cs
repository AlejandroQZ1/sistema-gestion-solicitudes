using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AdministracionSoluciones.Infraestructura;

// Convierte las horas del formulario sin depender del separador decimal del servidor.
public class HorasModelBinder : IModelBinder
{
    // Acepta coma o punto decimal y conserva los campos vacíos como valores nulos.
    public Task BindModelAsync(ModelBindingContext context)
    {
        var valor = context.ValueProvider.GetValue(context.ModelName);
        if (valor == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        context.ModelState.SetModelValue(context.ModelName, valor);
        string? texto = valor.FirstValue?.Trim();
        if (string.IsNullOrEmpty(texto))
        {
            context.Result = ModelBindingResult.Success(null);
            return Task.CompletedTask;
        }

        // Los separadores de miles no forman parte del formato admitido.
        texto = texto.Replace(',', '.');
        var formato = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;
        if (decimal.TryParse(texto, formato, CultureInfo.InvariantCulture, out decimal horas))
        {
            context.Result = ModelBindingResult.Success(horas);
        }
        else
        {
            context.ModelState.TryAddModelError(context.ModelName, "Indique un número de horas válido.");
        }

        return Task.CompletedTask;
    }
}

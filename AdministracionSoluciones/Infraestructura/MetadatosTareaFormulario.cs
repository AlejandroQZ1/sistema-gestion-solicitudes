using AdministracionSoluciones.Entities.Tareas;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AdministracionSoluciones.Infraestructura;

// Configuración de formularios que pertenece a la web, no a las entidades.
public class MetadatosTareaFormulario : IBindingMetadataProvider, IValidationMetadataProvider
{
    public void CreateBindingMetadata(BindingMetadataProviderContext context)
    {
        if (context.Key.ContainerType == typeof(TareaFormulario)
            && context.Key.Name == nameof(TareaFormulario.Horas))
        {
            context.BindingMetadata.BinderType = typeof(HorasModelBinder);
        }
    }

    public void CreateValidationMetadata(ValidationMetadataProviderContext context)
    {
        if (context.Key.ContainerType == typeof(TareaFormulario)
            && context.Key.Name is nameof(TareaFormulario.TareaId)
                or nameof(TareaFormulario.Solicitud)
                or nameof(TareaFormulario.Usuarios))
        {
            // Estos datos se cargan en el servidor y no son campos editables.
            context.ValidationMetadata.PropertyValidationFilter = new ValidateNeverAttribute();
        }
    }
}

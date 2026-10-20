using MySqlConnector;
using AdministracionSoluciones.Entities;
using AdministracionSoluciones.Repository;
using AdministracionSoluciones.Services.Abstract;

namespace AdministracionSoluciones.Services
{
    /// <summary>
    /// SOL2 - Reglas de negocio para la administración de desgloses de solicitudes.
    /// Valida los datos y registra las operaciones en bitácora.
    /// </summary>
    public class DesgloseService : IDesgloseService
    {
        private const string Modulo = "Desgloses";
        public const string MensajeDatosRelacionados = "No se puede eliminar un registro con datos relacionados.";

        private readonly DesgloseRepository _desgloseRepository;
        private readonly IBitacoraService _bitacoraService;

        public DesgloseService(DesgloseRepository desgloseRepository, IBitacoraService bitacoraService)
        {
            _desgloseRepository = desgloseRepository;
            _bitacoraService = bitacoraService;
        }

        public async Task<IEnumerable<Desglose>> GetAllAsync(int? solicitudId = null, string? usuarioActual = null)
        {
            try
            {
                var desgloses = await _desgloseRepository.GetAllAsync(solicitudId);

                if (!string.IsNullOrWhiteSpace(usuarioActual))
                {
                    var detalleConsulta = solicitudId.HasValue
                        ? $"El usuario consulta desgloses de la solicitud con ID {solicitudId}."
                        : "El usuario consulta listado general de desgloses.";
                    await _bitacoraService.RegistrarAsync(usuarioActual, Modulo, "Consultar", detalleConsulta);
                }

                return desgloses;
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrWhiteSpace(usuarioActual))
                {
                    await _bitacoraService.RegistrarAsync(usuarioActual, Modulo, "Error Técnico", new { Error = ex.Message });
                }
                throw;
            }
        }

        public async Task<Desglose?> GetByIdAsync(int id, string? usuarioActual = null)
        {
            try
            {
                var desglose = await _desgloseRepository.GetByIdAsync(id);
                if (desglose != null && !string.IsNullOrWhiteSpace(usuarioActual))
                {
                    await _bitacoraService.RegistrarAsync(usuarioActual, Modulo, "Consultar", $"El usuario consulta desglose ID {id}.");
                }
                return desglose;
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrWhiteSpace(usuarioActual))
                {
                    await _bitacoraService.RegistrarAsync(usuarioActual, Modulo, "Error Técnico", new { Error = ex.Message });
                }
                throw;
            }
        }

        public async Task<Resultado> CrearAsync(Desglose desglose, string usuarioActual)
        {
            try
            {
                var error = Validar(desglose);
                if (error != null)
                {
                    return error;
                }

                // Cálculo automático de IVA (13%) y Total (Monto + IVA)
                CalcularTotales(desglose);

                desglose.DesgloseID = await _desgloseRepository.InsertAsync(desglose);

                // La bitácora utiliza los importes calculados por la base.
                var guardado = await _desgloseRepository.GetByIdAsync(desglose.DesgloseID)
                    ?? throw new InvalidOperationException("No se pudo consultar el desglose guardado.");

                await _bitacoraService.RegistrarAsync(usuarioActual, Modulo, "Crear", DatosDesglose(guardado));
                return Resultado.Ok("El desglose se registró correctamente.");
            }
            catch (Exception ex)
            {
                await _bitacoraService.RegistrarAsync(usuarioActual, Modulo, "Error Técnico", new { Accion = "Crear", Error = ex.Message });
                return Resultado.Error($"Error al registrar el desglose: {ex.Message}");
            }
        }

        public async Task<Resultado> ActualizarAsync(Desglose datos, string usuarioActual)
        {
            try
            {
                var anterior = await _desgloseRepository.GetByIdAsync(datos.DesgloseID);
                if (anterior == null)
                {
                    return Resultado.Error("El desglose no existe.");
                }

                var error = Validar(datos);
                if (error != null)
                {
                    return error;
                }

                CalcularTotales(datos);

                await _desgloseRepository.UpdateAsync(datos);

                var guardado = await _desgloseRepository.GetByIdAsync(datos.DesgloseID)
                    ?? throw new InvalidOperationException("No se pudo consultar el desglose guardado.");

                await _bitacoraService.RegistrarAsync(usuarioActual, Modulo, "Actualizar", new
                {
                    Anterior = DatosDesglose(anterior),
                    Actual = DatosDesglose(guardado)
                });

                return Resultado.Ok("El desglose se actualizó correctamente.");
            }
            catch (Exception ex)
            {
                await _bitacoraService.RegistrarAsync(usuarioActual, Modulo, "Error Técnico", new { Accion = "Actualizar", Error = ex.Message });
                return Resultado.Error($"Error al actualizar el desglose: {ex.Message}");
            }
        }

        public async Task<Resultado> EliminarAsync(int id, string usuarioActual)
        {
            try
            {
                var desglose = await _desgloseRepository.GetByIdAsync(id);
                if (desglose == null)
                {
                    return Resultado.Error("El desglose no existe.");
                }

                // Las claves foráneas impiden borrar registros con datos relacionados.
                try
                {
                    await _desgloseRepository.DeleteAsync(id);
                }
                catch (MySqlException ex) when (ex.Number == 1451)
                {
                    return Resultado.Error(MensajeDatosRelacionados);
                }

                await _bitacoraService.RegistrarAsync(usuarioActual, Modulo, "Eliminar", DatosDesglose(desglose));
                return Resultado.Ok("El desglose se eliminó correctamente.");
            }
            catch (Exception ex)
            {
                await _bitacoraService.RegistrarAsync(usuarioActual, Modulo, "Error Técnico", new { Accion = "Eliminar", Error = ex.Message });
                return Resultado.Error($"Error al eliminar el desglose: {ex.Message}");
            }
        }

        public Task<IEnumerable<Solicitud>> GetSolicitudesParaComboAsync()
        {
            return _desgloseRepository.GetSolicitudesParaComboAsync();
        }

        public Task<Solicitud?> GetSolicitudByIdAsync(int solicitudId)
        {
            return _desgloseRepository.GetSolicitudByIdAsync(solicitudId);
        }

        // ---------- REGLAS Y CÁLCULOS ----------

        private static Resultado? Validar(Desglose d)
        {
            if (d.SolicitudID <= 0)
            {
                return Resultado.Error("Debe seleccionar una solicitud válida.", nameof(Desglose.SolicitudID));
            }
            if (d.Mes < 1 || d.Mes > 12)
            {
                return Resultado.Error("El mes debe estar comprendido entre 1 y 12.", nameof(Desglose.Mes));
            }
            if (d.Anio < 2000 || d.Anio > 2100)
            {
                return Resultado.Error("El año indicado no es válido.", nameof(Desglose.Anio));
            }
            if (d.Horas < 0)
            {
                return Resultado.Error("Las horas no pueden ser negativas.", nameof(Desglose.Horas));
            }
            if (d.Monto < 0)
            {
                return Resultado.Error("El monto no puede ser negativo.", nameof(Desglose.Monto));
            }
            if (d.PorcentajeCobro < 0 || d.PorcentajeCobro > 100)
            {
                return Resultado.Error("El porcentaje de cobro debe encontrarse entre 0% y 100%.", nameof(Desglose.PorcentajeCobro));
            }
            return null;
        }

        private static void CalcularTotales(Desglose d)
        {
            d.IVA = Math.Round(d.Monto * 0.13m, 2);
            d.Total = Math.Round(d.Monto + d.IVA, 2);
        }

        private static object DatosDesglose(Desglose d) => new
        {
            d.DesgloseID,
            d.SolicitudID,
            d.Mes,
            d.Anio,
            d.Horas,
            d.Monto,
            d.IVA,
            d.Total,
            d.Observaciones,
            d.PorcentajeCobro
        };
    }
}

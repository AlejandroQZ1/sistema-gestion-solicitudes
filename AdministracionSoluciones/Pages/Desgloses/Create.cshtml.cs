using AdministracionSoluciones.Entities;
using AdministracionSoluciones.Seguridad;
using AdministracionSoluciones.Services.Abstract;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AdministracionSoluciones.Pages.Desgloses
{
    /// <summary>
    /// SOL2 - Creación de un nuevo desglose de solicitud.
    /// </summary>
    public class CreateModel : PageModel
    {
        private readonly IDesgloseService _desgloseService;

        public CreateModel(IDesgloseService desgloseService)
        {
            _desgloseService = desgloseService;
            Desglose = new Desglose
            {
                Mes = DateTime.Now.Month,
                Anio = DateTime.Now.Year,
                Horas = 0,
                Monto = 0,
                PorcentajeCobro = 100
            };
        }

        [BindProperty]
        public Desglose Desglose { get; set; }

        public SelectList SolicitudesSelectList { get; set; } = null!;
        public SelectList MesesSelectList { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int? solicitudId)
        {
            if (solicitudId.HasValue)
            {
                Desglose.SolicitudID = solicitudId.Value;
            }

            await CargarListasAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await CargarListasAsync();
                return Page();
            }

            var usuario = Sesion.NombreUsuario(HttpContext);
            var resultado = await _desgloseService.CrearAsync(Desglose, usuario);

            if (!resultado.Exito)
            {
                if (!string.IsNullOrEmpty(resultado.Campo))
                {
                    ModelState.AddModelError($"Desglose.{resultado.Campo}", resultado.Mensaje);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, resultado.Mensaje);
                }

                await CargarListasAsync();
                return Page();
            }

            TempData["Exito"] = resultado.Mensaje;
            return RedirectToPage("Index", new { solicitudId = Desglose.SolicitudID });
        }

        private async Task CargarListasAsync()
        {
            var solicitudes = await _desgloseService.GetSolicitudesParaComboAsync();
            SolicitudesSelectList = new SelectList(
                solicitudes.Select(s => new { s.SolicitudID, Descripcion = $"{s.ConsecutivoOficio} — {s.Titulo}" }),
                "SolicitudID",
                "Descripcion",
                Desglose.SolicitudID
            );

            var meses = Enumerable.Range(1, 12).Select(m => new
            {
                Numero = m,
                Nombre = new DateTime(2026, m, 1).ToString("MMMM", new System.Globalization.CultureInfo("es-ES"))
            }).Select(m => new { m.Numero, Nombre = char.ToUpper(m.Nombre[0]) + m.Nombre.Substring(1) });

            MesesSelectList = new SelectList(meses, "Numero", "Nombre", Desglose.Mes);
        }
    }
}

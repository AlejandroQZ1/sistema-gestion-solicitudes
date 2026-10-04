using AdministracionSoluciones.Filters;
using AdministracionSoluciones.Models;
using AdministracionSoluciones.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace AdministracionSoluciones.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        /// <summary>USR2 - Página de bienvenida después del login.</summary>
        public IActionResult Index()
        {
            ViewBag.NombreCompleto = Sesion.NombreCompleto(HttpContext);
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [PermitirSinSesion]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

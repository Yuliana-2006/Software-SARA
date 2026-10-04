using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace Software_SARA.Controllers
{
    public class SedeViewModel
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = "";

        public string Codigo { get; set; } = "";

        public string CorreoElectronico { get; set; } = "";

        public string Departamento { get; set; } = "";

        public string CiudadMunicipio { get; set; } = "";

        public string Barrio { get; set; } = "";

        public string Direccion { get; set; } = "";

        public string Telefono { get; set; } = "";
    }


    public class SedesController : Controller
    {
        // Lista temporal de sedes
        private static List<SedeViewModel> _listaSedes = new List<SedeViewModel>();


        // GET: /Sedes/RegistrarSede
        [HttpGet]
        public IActionResult RegistrarSede()
        {
            return View(_listaSedes);
        }


        // GET: /Sedes/CrearSede
        [HttpGet]
        public IActionResult CrearSede()
        {
            return View();
        }


        // POST: /Sedes/CrearSede
        [HttpPost]
        [ValidateAntiForgeryToken]

        public IActionResult CrearSede(SedeViewModel sede)
        {
            if (!ModelState.IsValid)
            {
                return View(sede);
            }

            sede.Id = _listaSedes.Count + 1;

            _listaSedes.Add(sede);

            TempData["MensajeExito"] = "¡Sede registrada exitosamente!";

            return RedirectToAction(nameof(RegistrarSede));
        }
    }
}
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

        [HttpGet]
        public IActionResult EditarSede(int id)
        {
            var sede = _listaSedes
                .FirstOrDefault(s => s.Id == id);

            if (sede == null)
            {
                return RedirectToAction("RegistrarSede");
            }

            return View(sede);
        }


        // POST: /Sedes/Editar
        [HttpPost]
        public IActionResult EditarSede(SedeViewModel sedeActualizada)
        {
            if (ModelState.IsValid)
            {
                var sedeExistente = _listaSedes
                    .FirstOrDefault(s => s.Id == sedeActualizada.Id);

                if (sedeExistente != null)
                {
                    sedeExistente.Nombre =
                        sedeActualizada.Nombre;

                    sedeExistente.Codigo =
                        sedeActualizada.Codigo;

                    sedeExistente.CorreoElectronico =
                        sedeActualizada.CorreoElectronico;

                    sedeExistente.Departamento =
                        sedeActualizada.Departamento;

                    sedeExistente.CiudadMunicipio =
                        sedeActualizada.CiudadMunicipio;

                    sedeExistente.Barrio =
                        sedeActualizada.Barrio;

                    sedeExistente.Direccion =
                        sedeActualizada.Direccion;

                    sedeExistente.Telefono =
                        sedeActualizada.Telefono;

                    TempData["MensajeExito"] =
                        "¡Sede modificada exitosamente!";

                    return RedirectToAction("RegistrarSede");
                }
            }

            return View(sedeActualizada);
        }

        // GET: Sedes/EliminarSede/5
        [HttpGet]
        public IActionResult ConfirmarEliminarSede(int id)
        {
            var sede = _listaSedes.FirstOrDefault(s => s.Id == id);

            if (sede == null)
            {
                return RedirectToAction("RegistrarSede");
            }

            return View("EliminarSede", sede);
        }

        // POST: Sedes/EliminarSede
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EliminarSede(int id)
        {
            var sede = _listaSedes.FirstOrDefault(s => s.Id == id);
             
            if (sede != null)
            {
                _listaSedes.Remove(sede);

                TempData["MensajeExito"] = "¡Sede eliminada exitosamente!";
            }

            return RedirectToAction("RegistrarSede");
        }
    }
}
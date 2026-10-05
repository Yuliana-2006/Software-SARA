using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Software_SARA.Controllers
{
    public class GerenteViewModel
    {
        public int Id { get; set; }
        public string? Nombres { get; set; }
        public string? Apellidos { get; set; }
        public string? TipoDocumento { get; set; }
        public string? NumeroDocumento { get; set; }
        public string? Correo { get; set; }
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
        public string? Sede { get; set; }
        public DateTime? FechaNacimiento { get; set; }
    }

    public class GerentesController : Controller
    {
        // Lista temporal en memoria
        private static List<GerenteViewModel> _listaGerentes = new List<GerenteViewModel>();

        // GET: /Gerentes
        public IActionResult Index()
        {
            return View(_listaGerentes);
        }

        // GET: /Gerentes/Crear
        [HttpGet]
        public IActionResult Crear()
        {
            var model = new GerenteViewModel
            {
                Sede = "Sede Principal",
                TipoDocumento = "CC"
            };
            return View(model);
        }

        // POST: /Gerentes/Crear
        [HttpPost]
        public IActionResult Crear(GerenteViewModel gerente)
        {
            // --- VALIDACIÓN DE FECHA DE NACIMIENTO VÁLIDA ---
            if (gerente.FechaNacimiento.HasValue)
            {
                var hoy = DateTime.Today;
                var fecha = gerente.FechaNacimiento.Value;

                // Calcular edad exacta
                int edad = hoy.Year - fecha.Year;
                if (fecha.Date > hoy.AddYears(-edad)) edad--;

                if (fecha > hoy)
                {
                    TempData["MensajeError"] = "La fecha de nacimiento no puede ser una fecha futura.";
                    return RedirectToAction("Index");
                }
                if (edad < 18)
                {
                    TempData["MensajeError"] = "El gerente debe ser mayor de edad (mínimo 18 años).";
                    return RedirectToAction("Index");
                }
                if (edad > 100)
                {
                    TempData["MensajeError"] = "Por favor ingrese una fecha de nacimiento válida (edad mayor a 100 años no permitida).";
                    return RedirectToAction("Index");
                }
            }
            else
            {
                TempData["MensajeError"] = "Por favor ingrese una fecha de nacimiento válida.";
                return RedirectToAction("Index");
            }

            // Validar duplicados por Nombre/Apellido o por Documento
            bool yaExiste = _listaGerentes.Any(g =>
                (!string.IsNullOrEmpty(g.Nombres) && g.Nombres.Trim().Equals(gerente.Nombres?.Trim(), StringComparison.OrdinalIgnoreCase) &&
                 !string.IsNullOrEmpty(g.Apellidos) && g.Apellidos.Trim().Equals(gerente.Apellidos?.Trim(), StringComparison.OrdinalIgnoreCase))
                ||
                (!string.IsNullOrEmpty(g.NumeroDocumento) && g.NumeroDocumento.Trim() == gerente.NumeroDocumento?.Trim())
            );

            if (yaExiste)
            {
                TempData["MensajeError"] = "No se puede registrar el gerente. La persona o número de documento ya existe en el sistema.";
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                gerente.Id = _listaGerentes.Count > 0 ? _listaGerentes.Max(g => g.Id) + 1 : 1;
                _listaGerentes.Add(gerente);

                TempData["MensajeExito"] = "¡Gerente registrado exitosamente!";
                return RedirectToAction("Index");
            }

            return View(gerente);
        }

        // GET: /Gerentes/Editar/5
        [HttpGet]
        public IActionResult Editar(int id)
        {
            var gerente = _listaGerentes.FirstOrDefault(g => g.Id == id);
            if (gerente == null)
            {
                return RedirectToAction("Index");
            }
            return View(gerente);
        }

        // POST: /Gerentes/Editar/5
        [HttpPost]
        public IActionResult Editar(GerenteViewModel gerente)
        {
            // --- VALIDACIÓN DE FECHA DE NACIMIENTO EN EDICIÓN ---
            if (gerente.FechaNacimiento.HasValue)
            {
                var hoy = DateTime.Today;
                var fecha = gerente.FechaNacimiento.Value;

                int edad = hoy.Year - fecha.Year;
                if (fecha.Date > hoy.AddYears(-edad)) edad--;

                if (fecha > hoy || edad < 18 || edad > 100)
                {
                    TempData["MensajeError"] = "Por favor ingrese una fecha de nacimiento válida para un gerente (entre 18 y 100 años).";
                    return RedirectToAction("Index");
                }
            }

            bool documentoDuplicado = _listaGerentes.Any(g =>
                g.Id != gerente.Id &&
                !string.IsNullOrEmpty(g.NumeroDocumento) &&
                g.NumeroDocumento.Trim() == gerente.NumeroDocumento?.Trim()
            );

            if (documentoDuplicado)
            {
                TempData["MensajeError"] = "No se puede guardar los cambios. El número de documento pertenece a otro gerente.";
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                var existente = _listaGerentes.FirstOrDefault(g => g.Id == gerente.Id);
                if (existente != null)
                {
                    existente.Nombres = gerente.Nombres;
                    existente.Apellidos = gerente.Apellidos;
                    existente.TipoDocumento = gerente.TipoDocumento;
                    existente.NumeroDocumento = gerente.NumeroDocumento;
                    existente.Correo = gerente.Correo;
                    existente.Telefono = gerente.Telefono;
                    existente.Direccion = gerente.Direccion;
                    existente.Sede = gerente.Sede;
                    existente.FechaNacimiento = gerente.FechaNacimiento;

                    TempData["MensajeExito"] = "¡Gerente actualizado exitosamente!";
                }
                return RedirectToAction("Index");
            }

            return View(gerente);
        }

        // GET: /Gerentes/Eliminar/5
        [HttpGet]
        public IActionResult Eliminar(int id)
        {
            var gerente = _listaGerentes.FirstOrDefault(g => g.Id == id);
            if (gerente == null)
            {
                return RedirectToAction("Index");
            }
            return View(gerente);
        }

        // POST: /Gerentes/Eliminar
        [HttpPost, ActionName("Eliminar")]
        public IActionResult EliminarConfirmado(int id)
        {
            var gerente = _listaGerentes.FirstOrDefault(g => g.Id == id);
            if (gerente != null)
            {
                _listaGerentes.Remove(gerente);
                TempData["MensajeExito"] = "¡Gerente eliminado exitosamente!";
            }

            return RedirectToAction("Index");
        }
    }
}
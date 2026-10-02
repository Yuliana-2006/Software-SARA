using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using Software_SARA.Models;

namespace Software_SARA.Controllers
{
    public class PersonalController : Controller
    {
        // Lista temporal en memoria con datos iniciales de prueba
        private static List<Personal> listaPersonal = new List<Personal>
        {
            new Personal { Id = 1, Nombres = "Carlos", Apellidos = "Mendoza", Rol = "Tesorero", TipoDoc = "CC", NumeroDoc = "1012345678", SedeRegistro = "Sede Principal" },
            new Personal { Id = 2, Nombres = "Ana", Apellidos = "Ríos", Rol = "Empleado", TipoDoc = "CC", NumeroDoc = "1098765432", SedeRegistro = "Sede Principal" }
        };

        // 1. PANTALLA PRINCIPAL DE PERSONAL (Las 3 tarjetas: Tesorero, Empleado, +)
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // 2. SELECCIONAR QUÉ ROL SE REGISTRARÁ AL PULSAR (+)
        [HttpGet]
        public IActionResult SeleccionarRolRegistro()
        {
            return View();
        }

        // 3. TARJETAS POR SECCIÓN (Muestra los empleados registrados en Tesorero o Empleado)
        [HttpGet]
        public IActionResult SeleccionarRol(string rol = "Empleado")
        {
            var listaFiltrada = listaPersonal
                .Where(p => p.Rol != null && p.Rol.Equals(rol, StringComparison.OrdinalIgnoreCase))
                .ToList();

            ViewBag.RolActual = rol;
            ViewBag.NombreSede = HttpContext.Session.GetString("SedeNombre") ?? "Sede Principal";

            return View(listaFiltrada);
        }

        // 4. FORMULARIO REGISTRAR (GET)
        [HttpGet]
        public IActionResult Registrar(string rol = "Empleado")
        {
            string sede = HttpContext.Session.GetString("SedeNombre") ?? "Sede Principal";
            ViewBag.RolActual = rol;
            ViewBag.NombreSede = sede;

            var nuevo = new Personal
            {
                Rol = rol,
                SedeRegistro = sede,
                FechaNacimiento = DateTime.Now.AddYears(-18)
            };

            return View(nuevo);
        }

        // 5. GUARDAR EL REGISTRO (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Registrar(Personal personal)
        {
            if (ModelState.IsValid)
            {
                personal.Id = listaPersonal.Count > 0 ? listaPersonal.Max(p => p.Id) + 1 : 1;
                listaPersonal.Add(personal);

                // Redirige a la sección donde se agregó (Tesorero o Empleado)
                return RedirectToAction("SeleccionarRol", new { rol = personal.Rol });
            }

            ViewBag.RolActual = personal.Rol;
            ViewBag.NombreSede = personal.SedeRegistro ?? "Sede Principal";
            return View(personal);
        }

        // 6. CONSULTAR REGISTRO (GET)
        [HttpGet]
        public IActionResult Consultar(int id, string rol = "Empleado")
        {
            var persona = listaPersonal.FirstOrDefault(p => p.Id == id);
            if (persona == null)
            {
                return RedirectToAction("SeleccionarRol", new { rol = rol });
            }

            ViewBag.RolActual = rol;
            return View(persona);
        }

        // 7. EDITAR REGISTRO (GET)
        [HttpGet]
        public IActionResult Editar(int id, string rol = "Empleado")
        {
            var persona = listaPersonal.FirstOrDefault(p => p.Id == id);
            if (persona == null)
            {
                return RedirectToAction("SeleccionarRol", new { rol = rol });
            }

            ViewBag.RolActual = rol;
            return View(persona);
        }

        // 8. GUARDAR EDICIÓN (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Editar(Personal personal)
        {
            if (ModelState.IsValid)
            {
                var registroExistente = listaPersonal.FirstOrDefault(p => p.Id == personal.Id);
                if (registroExistente != null)
                {
                    registroExistente.Nombres = personal.Nombres;
                    registroExistente.Apellidos = personal.Apellidos;
                    registroExistente.TipoDoc = personal.TipoDoc;
                    registroExistente.NumeroDoc = personal.NumeroDoc;
                    registroExistente.Telefono = personal.Telefono;
                    registroExistente.Correo = personal.Correo;
                    registroExistente.Direccion = personal.Direccion;
                    registroExistente.FechaNacimiento = personal.FechaNacimiento;
                    registroExistente.SedeRegistro = personal.SedeRegistro;
                    registroExistente.Rol = personal.Rol;
                }

                return RedirectToAction("SeleccionarRol", new { rol = personal.Rol });
            }

            ViewBag.RolActual = personal.Rol;
            return View(personal);
        }

        // 9. ELIMINAR REGISTRO (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Eliminar(int id, string rol = "Empleado")
        {
            var persona = listaPersonal.FirstOrDefault(p => p.Id == id);
            if (persona != null)
            {
                listaPersonal.Remove(persona);
            }

            return RedirectToAction("SeleccionarRol", new { rol = rol });
        }
    }
}
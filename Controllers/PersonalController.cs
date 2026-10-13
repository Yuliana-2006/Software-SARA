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
        // Lista temporal en memoria (luego la reemplazaremos por la base de datos)
        private static List<Personal> listaPersonal = new List<Personal>
        {
            // Datos de prueba (puedes borrarlos si quieres empezar vacío)
            // new Personal { Id = 1, Nombres = "Juan", Apellidos = "Perez", Rol = "Tesorero", SedeRegistro = "Sede Principal", TipoDoc = "CC", NumeroDoc = "123456" }
        };

        // 1. PANTALLA PRINCIPAL DE PERSONAL (Tesorero, Empleado)
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // 2. TARJETAS POR SECCIÓN (Muestra los empleados registrados en Tesorero o Empleado)
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

        // 3. FORMULARIO REGISTRAR (GET)
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
                FechaNacimiento = DateTime.Now.AddYears(-18) // Fecha por defecto (18 años atrás)
            };

            return View(nuevo);
        }

        // 4. GUARDAR EL REGISTRO (POST)
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

        // 5. CONSULTAR REGISTRO (GET)
        [HttpGet]
        public IActionResult Consultar(int id)
        {
            // Aquí luego pondremos la lógica real para consultar un registro por ID
            return NoContent();
        }

        // 6. EDITAR REGISTRO (GET)
        [HttpGet]
        public IActionResult Editar(int id)
        {
            // Aquí luego pondremos la lógica real para cargar un registro y editarlo
            return NoContent();
        }

        // 7. GUARDAR EDICIÓN (POST)
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

        // 8. ELIMINAR REGISTRO (POST)
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
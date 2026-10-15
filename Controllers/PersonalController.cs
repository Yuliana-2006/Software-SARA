using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using Software_SARA.Repositories.Models;

namespace Software_SARA.Controllers
{
    public class PersonalController : Controller
    {
        // Lista temporal (luego la conectaremos a la base de datos)
        private static List<Usuario> listaPersonal = new List<Usuario>();

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult SeleccionarRol(string rol = "Empleado")
        {
            var listaFiltrada = listaPersonal.ToList();
            ViewBag.RolActual = rol;
            ViewBag.NombreSede = HttpContext.Session.GetString("SedeNombre") ?? "Sede Principal";
            return View(listaFiltrada);
        }

        [HttpGet]
        public IActionResult Registrar(string rol = "Empleado")
        {
            string sede = HttpContext.Session.GetString("SedeNombre") ?? "Sede Principal";
            ViewBag.RolActual = rol;
            ViewBag.NombreSede = sede;
            ViewBag.IdSede = 1;

            var nuevo = new Usuario
            {
                FechaNaci = DateOnly.FromDateTime(DateTime.Now.AddYears(-18))
            };
            return View(nuevo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Registrar(Usuario usuario, string tipoDocString)
        {
            usuario.TipDoc = tipoDocString == "CC";
            usuario.Contraseña = "1234";
            usuario.EstadoUsuario = true;

            usuario.IdUsuario = listaPersonal.Count > 0 ? listaPersonal.Max(p => p.IdUsuario) + 1 : 1;
            listaPersonal.Add(usuario);

            string nombreRol = usuario.IdRol == 3 ? "Tesorero" : "Empleado";
            return RedirectToAction("SeleccionarRol", new { rol = nombreRol });
        }
    }
}
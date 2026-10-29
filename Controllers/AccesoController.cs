using Microsoft.AspNetCore.Mvc;
using Software_SARA.Models;
using Software_SARA.Repositories.Models;
using Software_SARA.Utilities;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace Software_SARA.Controllers
{
    public class AccesoController : Controller
    {
        private readonly SarabdContext _context;

        public AccesoController(SarabdContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel modelo)
        {
            if (!ModelState.IsValid) return View(modelo);

            bool tipoDocBool = modelo.TipoDocumento == "CC";
            int numDocInt = int.Parse(modelo.NumeroDocumento);

            // 1. Buscar usuario
            var usuario = _context.Usuarios.FirstOrDefault(u => u.TipDoc == tipoDocBool && u.NumDoc == numDocInt);

            if (usuario == null)
            {
                ViewBag.Error = "Documento o contraseña incorrectos.";
                return View(modelo);
            }

            if (usuario.EstadoUsuario != true)
            {
                ViewBag.Error = "El usuario se encuentra inactivo.";
                return View(modelo);
            }

            // 2. Verificar Contraseña
            bool passwordValida = PasswordHelper.Verificar(modelo.Contrasena, usuario.Contraseña);

            if (!passwordValida)
            {
                ViewBag.Error = "Documento o contraseña incorrectos.";
                return View(modelo);
            }

            // 3. Guardar sesión
            HttpContext.Session.SetString("UsuarioId", usuario.IdUsuario.ToString());
            HttpContext.Session.SetString("UsuarioNombre", usuario.NomUsuario + " " + usuario.ApelliUsuario);
            HttpContext.Session.SetString("UsuarioDoc", usuario.NumDoc.ToString());

            var rol = _context.Rols.FirstOrDefault(r => r.IdRol == usuario.IdRol);
            string nombreRol = rol?.NomRol ?? "Sin Rol";

            // TRUCO: Si es el documento 99999, forzamos que sea Admin
            if (usuario.NumDoc == 99999) nombreRol = "Administrador";

            HttpContext.Session.SetString("UsuarioRol", nombreRol);

            var sede = _context.Sedes.FirstOrDefault(s => s.IdSede == usuario.IdSede);
            HttpContext.Session.SetString("SedeNombre", sede?.NomSede ?? "Sede Principal");

            // 4. Redirección
            if (nombreRol == "Administrador")
                return RedirectToAction("Index", "Dueno");
            else if (nombreRol == "Gerente")
                return RedirectToAction("Index", "Personal");
            else
                return RedirectToAction("Index", "Home");
        }

        public IActionResult CerrarSesion()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
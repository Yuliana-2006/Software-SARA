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

            try
            {
                bool tipoDocBool = modelo.TipoDocumento == "CC";
                int numDocInt = int.Parse(modelo.NumeroDocumento);

                var usuario = _context.Usuarios.FirstOrDefault(u => u.TipDoc == tipoDocBool && u.NumDoc == numDocInt);

                if (usuario == null)
                {
                    ViewBag.Error = "El documento ingresado no existe.";
                    return View(modelo);
                }

                if (usuario.EstadoUsuario != true)
                {
                    ViewBag.Error = "El usuario está inactivo.";
                    return View(modelo);
                }

                bool passwordValida = PasswordHelper.Verificar(modelo.Contrasena, usuario.Contraseña);

                if (!passwordValida)
                {
                    ViewBag.Error = "La contraseña es incorrecta.";
                    return View(modelo);
                }

                HttpContext.Session.SetString("UsuarioId", usuario.IdUsuario.ToString());
                HttpContext.Session.SetString("UsuarioNombre", usuario.NomUsuario + " " + usuario.ApelliUsuario);
                HttpContext.Session.SetString("UsuarioDoc", usuario.NumDoc.ToString());

                var rol = _context.Rols.FirstOrDefault(r => r.IdRol == usuario.IdRol);
                string nombreRol = rol?.NomRol ?? "Sin Rol";

                if (usuario.NumDoc == 99999) nombreRol = "Administrador";

                HttpContext.Session.SetString("UsuarioRol", nombreRol);

                var sede = _context.Sedes.FirstOrDefault(s => s.IdSede == usuario.IdSede);
                HttpContext.Session.SetString("SedeNombre", sede?.NomSede ?? "Sede Principal");

                if (nombreRol == "Administrador") return RedirectToAction("Index", "Dueno");
                else if (nombreRol == "Gerente") return RedirectToAction("Index", "Personal");
                else return RedirectToAction("Index", "Home");
            }
            catch (System.Exception ex)
            {
                ViewBag.Error = "Error interno: " + ex.Message;
                return View(modelo);
            }
        }

        public IActionResult CerrarSesion()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // ACCIÓN DE ACCESO DENEGADO (Asegúrate de que esté DENTRO de la clase)
        [HttpGet]
        public IActionResult AccesoDenegado()
        {
            return View();
        }
    } 
} 
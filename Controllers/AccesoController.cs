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
            if (!ModelState.IsValid)
                return View(modelo);

            bool tipoDocBool = modelo.TipoDocumento == "CC";
            int numDocInt = int.Parse(modelo.NumeroDocumento);

            // Buscar al usuario por documento (sin comparar contraseña aún)
            var usuario = _context.Usuarios.FirstOrDefault(u =>
                u.TipDoc == tipoDocBool &&
                u.NumDoc == numDocInt &&
                u.EstadoUsuario == true);

            if (usuario != null)
            {
                // Verificar contraseña ENCRIPTADA
                bool passwordValida = PasswordHelper.Verificar(modelo.Contrasena, usuario.Contraseña);

                if (passwordValida)
                {
                    // Guardar datos en la sesión
                    HttpContext.Session.SetString("UsuarioId", usuario.IdUsuario.ToString());
                    HttpContext.Session.SetString("UsuarioNombre", usuario.NomUsuario + " " + usuario.ApelliUsuario);
                    HttpContext.Session.SetString("UsuarioDoc", usuario.NumDoc.ToString());

                    var rol = _context.Rols.FirstOrDefault(r => r.IdRol == usuario.IdRol);
                    HttpContext.Session.SetString("UsuarioRol", rol?.NomRol ?? "Sin Rol");

                    var sede = _context.Sedes.FirstOrDefault(s => s.IdSede == usuario.IdSede);
                    HttpContext.Session.SetString("SedeNombre", sede?.NomSede ?? "Sede Principal");
                    HttpContext.Session.SetString("SedeId", usuario.IdSede.ToString());

                    // Redirigir según el rol
                    string nombreRol = rol?.NomRol ?? "";

                    if (nombreRol == "Administrador")
                        return RedirectToAction("Index", "Dueno");
                    else if (nombreRol == "Dueño")
                        return RedirectToAction("Index", "Home");
                    else if (nombreRol == "Gerente")
                        return RedirectToAction("Index", "Personal");
                    else if (nombreRol == "Tesorero")
                        return RedirectToAction("Index", "Home");
                    else if (nombreRol == "Empleado")
                        return RedirectToAction("Index", "Home");

                    return RedirectToAction("Index", "Home");
                }
            }

            ViewBag.Error = "Documento,contraseña incorrectos o usuario inactivo.";
            return View(modelo);
        }

        public IActionResult CerrarSesion()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
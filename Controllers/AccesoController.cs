using Microsoft.AspNetCore.Mvc;
using Software_SARA.Models;
using Software_SARA.Repositories.Models;
using Software_SARA.Utilities;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System;

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

            // Validamos que el número de documento sea un entero válido
            if (!int.TryParse(modelo.NumeroDocumento, out int numDocInt))
            {
                ViewBag.Error = "Número de documento inválido.";
                return View(modelo);
            }

            // Buscar al usuario por documento y estado activo
            var usuario = _context.Usuarios.FirstOrDefault(u =>
                u.TipDoc == tipoDocBool &&
                u.NumDoc == numDocInt &&
                u.EstadoUsuario == true);

            if (usuario == null)
            {
                ViewBag.Error = "Documento o contraseña incorrectos, o usuario inactivo.";
                return View(modelo);
            }

            bool passwordValida = false;

            try
            {
                // 1. Intentar verificar si la contraseña usa el hash seguro de BCrypt
                passwordValida = PasswordHelper.Verificar(modelo.Contrasena, usuario.Contraseña);
            }
            catch
            {
                passwordValida = false;
            }

            // 2. Si no pasó por BCrypt (cuentas antiguas de texto plano), comprobamos si coincide directo
            if (!passwordValida)
            {
                passwordValida = (modelo.Contrasena == usuario.Contraseña);
            }

            if (!passwordValida)
            {
                ViewBag.Error = "Documento o contraseña incorrectos.";
                return View(modelo);
            }

            // 3. Guardar datos en la sesión
            HttpContext.Session.SetString("UsuarioId", usuario.IdUsuario.ToString());
            HttpContext.Session.SetString("UsuarioNombre", usuario.NomUsuario + " " + usuario.ApelliUsuario);
            HttpContext.Session.SetString("UsuarioDoc", usuario.NumDoc.ToString());

            var rol = _context.Rols.FirstOrDefault(r => r.IdRol == usuario.IdRol);
            string nombreRol = rol?.NomRol?.Trim() ?? "Sin Rol";

            // TRUCO: Si es el documento 99999, forzamos que sea Admin
            if (usuario.NumDoc == 99999) nombreRol = "Administrador";

            HttpContext.Session.SetString("UsuarioRol", nombreRol);

            var sede = _context.Sedes.FirstOrDefault(s => s.IdSede == usuario.IdSede);
            HttpContext.Session.SetString("SedeNombre", sede?.NomSede ?? "Sede Principal");
            HttpContext.Session.SetString("SedeId", usuario.IdSede.ToString());

            // 4. Redirección según el rol de forma segura
            if (nombreRol.Equals("Administrador", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Index", "Dueno");
            else if (nombreRol.Equals("Dueño", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Index", "Home");
            else if (nombreRol.Equals("Gerente", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Index", "Home");
            else if (nombreRol.Equals("Tesorero", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Index", "Home");
            else if (nombreRol.Equals("Empleado", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Index", "Home");

            return RedirectToAction("Index", "Home");
        }

        public IActionResult CerrarSesion()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
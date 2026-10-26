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
        public IActionResult Login(LoginViewModel modelo, string NumeroDocumento, string Contrasena)
        {
            string numDocStr = modelo?.NumeroDocumento ?? NumeroDocumento;
            string passStr = modelo?.Contrasena ?? Contrasena;

            if (string.IsNullOrEmpty(numDocStr))
            {
                ViewBag.Error = "Por favor ingrese el número de documento.";
                return View(modelo);
            }

            string docLimpio = numDocStr.Trim();
            if (!long.TryParse(docLimpio, out long numDocLong))
            {
                ViewBag.Error = "Documento o contraseña incorrectos.";
                return View(modelo);
            }

            var usuario = _context.Usuarios.FirstOrDefault(u => u.NumDoc == numDocLong);

            if (usuario != null)
            {
                bool passwordValida = false;

                if (usuario.Contraseña == passStr)
                {
                    passwordValida = true;
                }
                else
                {
                    try
                    {
                        passwordValida = PasswordHelper.Verificar(passStr, usuario.Contraseña);
                    }
                    catch
                    {
                        passwordValida = false;
                    }
                }

                if (passwordValida)
                {
                    string nombreCompleto = (usuario.NomUsuario ?? "") + " " + (usuario.ApelliUsuario ?? "");

                    HttpContext.Session.SetString("UsuarioId", usuario.IdUsuario.ToString());
                    HttpContext.Session.SetString("UsuarioNombre", nombreCompleto);
                    HttpContext.Session.SetString("UsuarioDoc", usuario.NumDoc.ToString());
                    HttpContext.Session.SetString("UsuarioRol", "Dueño");

                    // Pantalla de éxito directa para confirmar el acceso sin rebotes
                    string htmlExito = $@"
                    <html>
                    <head>
                        <meta charset='utf-8'>
                        <title>Acceso Exitoso - SARA</title>
                    </head>
                    <body style='background-color: #1b4d3e; color: white; text-align: center; padding-top: 80px; font-family: Arial, sans-serif;'>
                        <div style='background: #113228; display: inline-block; padding: 40px; border-radius: 15px; box-shadow: 0px 4px 15px rgba(0,0,0,0.3);'>
                            <h1 style='color: #a8f5d0;'>¡Inicio de Sesión Exitoso!</h1>
                            <p style='font-size: 18px;'>Bienvenido, <b>{nombreCompleto}</b> (Dueño).</p>
                            <p style='margin: 30px 0;'>Tu sesión está activa correctamente.</p>
                            <a href='/Dueno/Index' style='background: #2d6a4f; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-weight: bold; display: inline-block;'>Ir al Panel de Dueño</a>
                        </div>
                    </body>
                    </html>";

                    return RedirectToAction("Index", "Dueno");
                }
            }

            ViewBag.Error = "Documento, contraseña incorrectos o usuario inactivo.";
            return View(modelo);
        }

        public IActionResult CerrarSesion()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
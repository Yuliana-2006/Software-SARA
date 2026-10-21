using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Software_SARA.Models;
using Software_SARA.Repositories.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BCrypt.Net;

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
        private readonly SarabdContext _context;

        public GerentesController(SarabdContext context)
        {
            _context = context;
        }

        // GET: /Gerentes
        public async Task<IActionResult> Index()
        {
            try
            {
                var listaGerentes = await _context.Usuarios
                    .Include(u => u.IdSedeNavigation)
                    .Include(u => u.IdRolNavigation)
                    .Where(u => u.IdRolNavigation != null && u.IdRolNavigation.NomRol == "Gerente")
                    .Select(u => new GerenteViewModel
                    {
                        Id = u.IdUsuario,
                        Nombres = u.NomUsuario,
                        Apellidos = u.ApelliUsuario,
                        TipoDocumento = u.TipDoc ? "1" : "0",
                        NumeroDocumento = u.NumDoc.ToString(),
                        Correo = u.Correo,
                        Telefono = u.Telefono,
                        Direccion = u.DirUsuario,
                        Sede = u.IdSedeNavigation != null ? u.IdSedeNavigation.NomSede : "Sede Principal",
                        FechaNacimiento = u.FechaNaci.ToDateTime(TimeOnly.MinValue)
                    })
                    .ToListAsync();

                return View(listaGerentes);
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error al conectar con la base de datos: " + ex.Message;
                return View(new List<GerenteViewModel>());
            }
        }

        // GET: /Gerentes/Crear
        [HttpGet]
        public IActionResult Crear()
        {
            var model = new GerenteViewModel
            {
                Sede = "Sede Principal",
                TipoDocumento = "1"
            };
            return View(model);
        }

        // POST: /Gerentes/Crear
        [HttpPost]
        public async Task<IActionResult> Crear(GerenteViewModel gerente)
        {
            try
            {
                if (gerente.FechaNacimiento.HasValue)
                {
                    var hoy = DateTime.Today;
                    var fecha = gerente.FechaNacimiento.Value;
                    int edad = hoy.Year - fecha.Year;
                    if (fecha.Date > hoy.AddYears(-edad)) edad--;

                    if (fecha > hoy || edad < 18 || edad > 100)
                    {
                        TempData["MensajeError"] = "Por favor ingrese una fecha de nacimiento válida (entre 18 y 100 años).";
                        return RedirectToAction("Index");
                    }
                }
                else
                {
                    TempData["MensajeError"] = "Por favor ingrese una fecha de nacimiento válida.";
                    return RedirectToAction("Index");
                }

                if (string.IsNullOrEmpty(gerente.Telefono) || gerente.Telefono.Length != 10 || !gerente.Telefono.All(char.IsDigit))
                {
                    TempData["MensajeError"] = "El número de teléfono debe contener exactamente 10 dígitos numéricos.";
                    return RedirectToAction("Index");
                }

                if (string.IsNullOrEmpty(gerente.NumeroDocumento) || gerente.NumeroDocumento.Length < 8 || gerente.NumeroDocumento.Length > 10 || !gerente.NumeroDocumento.All(char.IsDigit))
                {
                    TempData["MensajeError"] = "El número de documento debe contener entre 8 y 10 dígitos numéricos.";
                    return RedirectToAction("Index");
                }

                if (string.IsNullOrEmpty(gerente.Correo) || !gerente.Correo.Contains("@"))
                {
                    TempData["MensajeError"] = "Por favor ingrese un correo electrónico válido (debe incluir '@').";
                    return RedirectToAction("Index");
                }

                int.TryParse(gerente.NumeroDocumento, out int numDocParsed);

                bool yaExiste = await _context.Usuarios.AnyAsync(g =>
                    (!string.IsNullOrEmpty(g.NomUsuario) && g.NomUsuario.ToLower().Trim() == gerente.Nombres!.ToLower().Trim() &&
                     !string.IsNullOrEmpty(g.ApelliUsuario) && g.ApelliUsuario.ToLower().Trim() == gerente.Apellidos!.ToLower().Trim())
                    ||
                    g.NumDoc == numDocParsed
                );

                if (yaExiste)
                {
                    TempData["MensajeError"] = "No se puede registrar el gerente. La persona o número de documento ya existe en el sistema.";
                    return RedirectToAction("Index");
                }

                if (ModelState.IsValid)
                {
                    var rolGerente = await _context.Rols.FirstOrDefaultAsync(r => r.NomRol == "Gerente");
                    var sedeDefault = await _context.Sedes.FirstOrDefaultAsync();

                    bool tipDocBool = gerente.TipoDocumento == "1" || gerente.TipoDocumento?.ToUpper() == "CC" || gerente.TipoDocumento?.ToLower() == "true";

                    var nuevoUsuario = new Usuario
                    {
                        NomUsuario = gerente.Nombres ?? string.Empty,
                        ApelliUsuario = gerente.Apellidos ?? string.Empty,
                        TipDoc = tipDocBool,
                        NumDoc = numDocParsed,
                        Correo = gerente.Correo ?? string.Empty,
                        Telefono = gerente.Telefono ?? string.Empty,
                        DirUsuario = gerente.Direccion ?? string.Empty,
                        Contraseña = BCrypt.Net.BCrypt.HashPassword("123"),
                        EstadoUsuario = true,
                        FechaNaci = DateOnly.FromDateTime(gerente.FechaNacimiento.Value),
                        IdRol = rolGerente != null ? rolGerente.IdRol : 2,
                        IdSede = sedeDefault != null ? sedeDefault.IdSede : 1
                    };

                    _context.Usuarios.Add(nuevoUsuario);
                    await _context.SaveChangesAsync();

                    TempData["MensajeExito"] = "¡Gerente registrado exitosamente!";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Ocurrió un error al intentar guardar el gerente: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // GET: /Gerentes/Editar/5
        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            try
            {
                var u = await _context.Usuarios.FindAsync(id);
                if (u == null)
                {
                    return RedirectToAction("Index");
                }

                var gerenteViewModel = new GerenteViewModel
                {
                    Id = u.IdUsuario,
                    Nombres = u.NomUsuario,
                    Apellidos = u.ApelliUsuario,
                    TipoDocumento = u.TipDoc ? "1" : "0",
                    NumeroDocumento = u.NumDoc.ToString(),
                    Correo = u.Correo,
                    Telefono = u.Telefono,
                    Direccion = u.DirUsuario,
                    FechaNacimiento = u.FechaNaci.ToDateTime(TimeOnly.MinValue)
                };

                return View(gerenteViewModel);
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error al consultar el gerente: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST: /Gerentes/Editar/5
        [HttpPost]
        public async Task<IActionResult> Editar(GerenteViewModel gerente)
        {
            try
            {
                if (gerente.FechaNacimiento.HasValue)
                {
                    var hoy = DateTime.Today;
                    var fecha = gerente.FechaNacimiento.Value;
                    int edad = hoy.Year - fecha.Year;
                    if (fecha.Date > hoy.AddYears(-edad)) edad--;

                    if (fecha > hoy || edad < 18 || edad > 100)
                    {
                        TempData["MensajeError"] = "Por favor ingrese una fecha de nacimiento válida (entre 18 y 100 años).";
                        return RedirectToAction("Index");
                    }
                }
                else
                {
                    TempData["MensajeError"] = "Por favor ingrese una fecha de nacimiento válida.";
                    return RedirectToAction("Index");
                }

                if (string.IsNullOrEmpty(gerente.Telefono) || gerente.Telefono.Length != 10 || !gerente.Telefono.All(char.IsDigit))
                {
                    TempData["MensajeError"] = "El número de teléfono debe contener exactamente 10 dígitos numéricos.";
                    return RedirectToAction("Index");
                }

                if (string.IsNullOrEmpty(gerente.NumeroDocumento) || gerente.NumeroDocumento.Length < 8 || gerente.NumeroDocumento.Length > 10 || !gerente.NumeroDocumento.All(char.IsDigit))
                {
                    TempData["MensajeError"] = "El número de documento debe contener entre 8 y 10 dígitos numéricos.";
                    return RedirectToAction("Index");
                }

                if (string.IsNullOrEmpty(gerente.Correo) || !gerente.Correo.Contains("@"))
                {
                    TempData["MensajeError"] = "Por favor ingrese un correo electrónico válido (debe incluir '@').";
                    return RedirectToAction("Index");
                }

                int.TryParse(gerente.NumeroDocumento, out int numDocParsed);

                bool documentoDuplicado = await _context.Usuarios.AnyAsync(g =>
                    g.IdUsuario != gerente.Id &&
                    g.NumDoc == numDocParsed
                );

                if (documentoDuplicado)
                {
                    TempData["MensajeError"] = "No se puede guardar los cambios. El número de documento pertenece a otro gerente.";
                    return RedirectToAction("Index");
                }

                if (ModelState.IsValid)
                {
                    var existente = await _context.Usuarios.FindAsync(gerente.Id);
                    if (existente != null)
                    {
                        bool tipDocBool = gerente.TipoDocumento == "1" || gerente.TipoDocumento?.ToUpper() == "CC" || gerente.TipoDocumento?.ToLower() == "true";

                        existente.NomUsuario = gerente.Nombres ?? existente.NomUsuario;
                        existente.ApelliUsuario = gerente.Apellidos ?? existente.ApelliUsuario;
                        existente.TipDoc = tipDocBool;
                        existente.NumDoc = numDocParsed;
                        existente.Correo = gerente.Correo ?? existente.Correo;
                        existente.Telefono = gerente.Telefono ?? existente.Telefono;
                        existente.DirUsuario = gerente.Direccion ?? existente.DirUsuario;
                        existente.FechaNaci = DateOnly.FromDateTime(gerente.FechaNacimiento!.Value);

                        _context.Usuarios.Update(existente);
                        await _context.SaveChangesAsync();

                        TempData["MensajeExito"] = "¡Gerente actualizado exitosamente!";
                    }
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Ocurrió un error al actualizar los datos: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // GET: /Gerentes/Eliminar/5
        [HttpGet]
        public async Task<IActionResult> Eliminar(int id)
        {
            try
            {
                var u = await _context.Usuarios.FindAsync(id);
                if (u == null)
                {
                    return RedirectToAction("Index");
                }

                var gerenteViewModel = new GerenteViewModel
                {
                    Id = u.IdUsuario,
                    Nombres = u.NomUsuario,
                    Apellidos = u.ApelliUsuario,
                    NumeroDocumento = u.NumDoc.ToString()
                };

                return View(gerenteViewModel);
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error al intentar consultar el registro a eliminar: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST: /Gerentes/Eliminar
        [HttpPost, ActionName("Eliminar")]
        public async Task<IActionResult> EliminarConfirmado(int id)
        {
            try
            {
                var usuario = await _context.Usuarios.FindAsync(id);
                if (usuario != null)
                {
                    _context.Usuarios.Remove(usuario);
                    await _context.SaveChangesAsync();
                    TempData["MensajeExito"] = "¡Gerente eliminado exitosamente!";
                }
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "No se pudo eliminar el gerente: " + ex.Message;
            }

            return RedirectToAction("Index");
        }
    }
}
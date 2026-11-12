using Software_SARA.Repositories.Models;
using Software_SARA.Utilities;
using Software_SARA.Dtos; // <--- Importamos la capa DTO
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Software_SARA.Services
{
    public class GerenteService
    {
        private readonly SarabdContext _context;
        private readonly EmailService _emailService;

        public GerenteService(SarabdContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<List<GerenteDto>> ObtenerGerentesAsync()
        {
            var gerentes = await _context.Usuarios.Where(u => u.IdRol == 2).ToListAsync();
            var sedesDict = _context.Sedes.ToDictionary(s => s.IdSede, s => s.NomSede);

            return gerentes.Select(g => new GerenteDto
            {
                Id = g.IdUsuario,
                Nombres = g.NomUsuario ?? string.Empty,
                Apellidos = g.ApelliUsuario ?? string.Empty,
                NumeroDocumento = g.NumDoc.ToString(),
                Correo = g.Correo ?? string.Empty,
                Telefono = g.Telefono ?? string.Empty,
                Direccion = g.DirUsuario ?? string.Empty,
                IdSede = g.IdSede,
                Sede = sedesDict.ContainsKey(g.IdSede) ? sedesDict[g.IdSede] : string.Empty,
                FechaNacimiento = g.FechaNaci.ToDateTime(TimeOnly.MinValue)
            }).ToList();
        }

        public async Task<GerenteDto?> ObtenerGerentePorIdAsync(int id)
        {
            var g = await _context.Usuarios.FindAsync(id);
            if (g == null) return null;

            var nombreSede = _context.Sedes.Where(s => s.IdSede == g.IdSede).Select(s => s.NomSede).FirstOrDefault() ?? string.Empty;

            return new GerenteDto
            {
                Id = g.IdUsuario,
                Nombres = g.NomUsuario ?? string.Empty,
                Apellidos = g.ApelliUsuario ?? string.Empty,
                NumeroDocumento = g.NumDoc.ToString(),
                Correo = g.Correo ?? string.Empty,
                Telefono = g.Telefono ?? string.Empty,
                Direccion = g.DirUsuario ?? string.Empty,
                IdSede = g.IdSede,
                Sede = nombreSede,
                FechaNacimiento = g.FechaNaci.ToDateTime(TimeOnly.MinValue)
            };
        }

        public async Task<string?> ValidarYCrearGerenteAsync(GerenteDto gerente)
        {
            if (_context.Usuarios.Any(u => u.NumDoc.ToString() == gerente.NumeroDocumento))
                return "El número de documento ya está registrado en el sistema.";
            if (_context.Usuarios.Any(u => u.Correo == gerente.Correo))
                return "El correo electrónico ya está registrado en el sistema.";

            var nuevoUsuario = new Usuario
            {
                TipDoc = (gerente.TipoDocumento == "CC" || gerente.TipoDocumento == "1"),
                NumDoc = int.Parse(gerente.NumeroDocumento ?? "0"),
                NomUsuario = gerente.Nombres ?? string.Empty,
                ApelliUsuario = gerente.Apellidos ?? string.Empty,
                Correo = gerente.Correo ?? string.Empty,
                Telefono = gerente.Telefono ?? string.Empty,
                DirUsuario = gerente.Direccion ?? string.Empty,
                FechaNaci = DateOnly.FromDateTime(gerente.FechaNacimiento ?? DateTime.Now.AddYears(-18)),
                IdRol = 2,
                IdSede = gerente.IdSede,
                EstadoUsuario = true
            };

            string passwordPlano = PasswordHelper.GenerarPassword(8);
            nuevoUsuario.Contraseña = PasswordHelper.Encriptar(passwordPlano);

            try
            {
                _context.Usuarios.Add(nuevoUsuario);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return "Error al guardar en BD: " + (ex.InnerException?.Message ?? ex.Message);
            }

            try
            {
                await _emailService.EnviarPasswordAsync(
                    nuevoUsuario.Correo,
                    nuevoUsuario.NomUsuario,
                    passwordPlano,
                    "Gerente",
                    nuevoUsuario.NumDoc.ToString()
                );
            }
            catch (Exception ex)
            {
                return $"Gerente guardado en BD, pero falló el envío del correo: {ex.Message}. Clave: {passwordPlano}";
            }

            return null;
        }

        public async Task<string?> ValidarYActualizarGerenteAsync(GerenteDto gerente)
        {
            var usuarioDb = await _context.Usuarios.FindAsync(gerente.Id);
            if (usuarioDb == null) return "El gerente no existe en la base de datos.";

            if (_context.Usuarios.Any(u => u.NumDoc.ToString() == gerente.NumeroDocumento && u.IdUsuario != gerente.Id))
                return "El número de documento ya pertenece a otro usuario.";
            if (_context.Usuarios.Any(u => u.Correo == gerente.Correo && u.IdUsuario != gerente.Id))
                return "El correo electrónico ya pertenece a otro usuario.";

            usuarioDb.TipDoc = (gerente.TipoDocumento == "CC" || gerente.TipoDocumento == "1");
            usuarioDb.NumDoc = int.Parse(gerente.NumeroDocumento ?? "0");
            usuarioDb.NomUsuario = gerente.Nombres ?? string.Empty;
            usuarioDb.ApelliUsuario = gerente.Apellidos ?? string.Empty;
            usuarioDb.Correo = gerente.Correo ?? string.Empty;
            usuarioDb.Telefono = gerente.Telefono ?? string.Empty;
            usuarioDb.DirUsuario = gerente.Direccion ?? string.Empty;
            usuarioDb.IdSede = gerente.IdSede;

            if (gerente.FechaNacimiento.HasValue)
                usuarioDb.FechaNaci = DateOnly.FromDateTime(gerente.FechaNacimiento.Value);

            try
            {
                _context.Usuarios.Update(usuarioDb);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return "Error al actualizar en BD: " + ex.Message;
            }
            return null;
        }

        public async Task EliminarGerenteAsync(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario != null)
            {
                _context.Usuarios.Remove(usuario);
                await _context.SaveChangesAsync();
            }
        }
    }
}
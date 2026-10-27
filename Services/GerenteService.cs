using Microsoft.EntityFrameworkCore;
using Software_SARA.Controllers;
using Software_SARA.Models;
using Software_SARA.Repositories.Models;
using Software_SARA.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Software_SARA.Services
{
    public class GerenteService
    {
        private readonly SarabdContext _context;

        public GerenteService(SarabdContext context)
        {
            _context = context;
        }

        public async Task<List<GerenteViewModel>> ObtenerGerentesAsync()
        {
            return await _context.Usuarios
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
        }

        public async Task<string?> ValidarYCrearGerenteAsync(GerenteViewModel gerente)
        {
            // Validaciones de negocio
            if (gerente.FechaNacimiento.HasValue)
            {
                var hoy = DateTime.Today;
                var fecha = gerente.FechaNacimiento.Value;
                int edad = hoy.Year - fecha.Year;
                if (fecha.Date > hoy.AddYears(-edad)) edad--;

                if (fecha > hoy || edad < 18 || edad > 100)
                    return "Por favor ingrese una fecha de nacimiento válida (entre 18 y 100 años).";
            }
            else
            {
                return "Por favor ingrese una fecha de nacimiento válida.";
            }

            if (string.IsNullOrEmpty(gerente.Telefono) || gerente.Telefono.Length != 10 || !gerente.Telefono.All(char.IsDigit))
                return "El número de teléfono debe contener exactamente 10 dígitos numéricos.";

            if (string.IsNullOrEmpty(gerente.NumeroDocumento) || gerente.NumeroDocumento.Length < 8 || gerente.NumeroDocumento.Length > 10 || !gerente.NumeroDocumento.All(char.IsDigit))
                return "El número de documento debe contener entre 8 y 10 dígitos numéricos.";

            if (string.IsNullOrEmpty(gerente.Correo) || !gerente.Correo.Contains("@"))
                return "Por favor ingrese un correo electrónico válido (debe incluir '@').";

            int.TryParse(gerente.NumeroDocumento, out int numDocParsed);

            bool yaExiste = await _context.Usuarios.AnyAsync(g =>
                (!string.IsNullOrEmpty(g.NomUsuario) && g.NomUsuario.ToLower().Trim() == gerente.Nombres!.ToLower().Trim() &&
                 !string.IsNullOrEmpty(g.ApelliUsuario) && g.ApelliUsuario.ToLower().Trim() == gerente.Apellidos!.ToLower().Trim())
                ||
                g.NumDoc == numDocParsed
            );

            if (yaExiste)
                return "No se puede registrar el gerente. La persona o número de documento ya existe en el sistema.";

            var rolGerente = await _context.Rols.FirstOrDefaultAsync(r => r.NomRol == "Gerente");
            var sedeDefault = await _context.Sedes.FirstOrDefaultAsync();
            bool tipDocBool = gerente.TipoDocumento == "1" || gerente.TipoDocumento?.ToUpper() == "CC" || gerente.TipoDocumento?.ToLower() == "true";

            // Usando el PasswordHelper de Utilities para encriptar la clave por defecto "123"
            string contraseñaEncriptada = PasswordHelper.Encriptar("123");
            
            var nuevoUsuario = new Usuario
            {
                NomUsuario = gerente.Nombres ?? string.Empty,
                ApelliUsuario = gerente.Apellidos ?? string.Empty,
                TipDoc = tipDocBool,
                NumDoc = numDocParsed,
                Correo = gerente.Correo ?? string.Empty,
                Telefono = gerente.Telefono ?? string.Empty,
                DirUsuario = gerente.Direccion ?? string.Empty,
                Contraseña = contraseñaEncriptada,
                EstadoUsuario = true,
                FechaNaci = DateOnly.FromDateTime(gerente.FechaNacimiento.Value),
                IdRol = rolGerente != null ? rolGerente.IdRol : 2,
                IdSede = sedeDefault != null ? sedeDefault.IdSede : 1
            };

            _context.Usuarios.Add(nuevoUsuario);
            await _context.SaveChangesAsync();
            return null; // Éxito (sin errores)
        }

        public async Task<GerenteViewModel?> ObtenerGerentePorIdAsync(int id)
        {
            var u = await _context.Usuarios.FindAsync(id);
            if (u == null) return null;

            return new GerenteViewModel
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
        }

        public async Task<string?> ValidarYActualizarGerenteAsync(GerenteViewModel gerente)
        {
            if (gerente.FechaNacimiento.HasValue)
            {
                var hoy = DateTime.Today;
                var fecha = gerente.FechaNacimiento.Value;
                int edad = hoy.Year - fecha.Year;
                if (fecha.Date > hoy.AddYears(-edad)) edad--;

                if (fecha > hoy || edad < 18 || edad > 100)
                    return "Por favor ingrese una fecha de nacimiento válida (entre 18 y 100 años).";
            }
            else
            {
                return "Por favor ingrese una fecha de nacimiento válida.";
            }

            if (string.IsNullOrEmpty(gerente.Telefono) || gerente.Telefono.Length != 10 || !gerente.Telefono.All(char.IsDigit))
                return "El número de teléfono debe contener exactamente 10 dígitos numéricos.";

            if (string.IsNullOrEmpty(gerente.NumeroDocumento) || gerente.NumeroDocumento.Length < 8 || gerente.NumeroDocumento.Length > 10 || !gerente.NumeroDocumento.All(char.IsDigit))
                return "El número de documento debe contener entre 8 y 10 dígitos numéricos.";

            if (string.IsNullOrEmpty(gerente.Correo) || !gerente.Correo.Contains("@"))
                return "Por favor ingrese un correo electrónico válido (debe incluir '@').";

            int.TryParse(gerente.NumeroDocumento, out int numDocParsed);

            bool documentoDuplicado = await _context.Usuarios.AnyAsync(g =>
                g.IdUsuario != gerente.Id &&
                g.NumDoc == numDocParsed
            );

            if (documentoDuplicado)
                return "No se puede guardar los cambios. El número de documento pertenece a otro gerente.";

            var existente = await _context.Usuarios.FindAsync(gerente.Id);
            if (existente == null) return "El gerente no fue encontrado.";

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
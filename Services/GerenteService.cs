using Software_SARA.Repositories.Models;
using Software_SARA.Utilities;
using Software_SARA.Dtos;
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

        public async Task<string?> ValidarDependenciasGerenteAsync(int idUsuario)
        {
            var gerenteDb = await _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);

            if (gerenteDb == null)
                return null;

            bool tieneFacturas = await _context.Facturas
                .AsNoTracking()
                .AnyAsync(f => f.IdUsuario == idUsuario);
            if (tieneFacturas)
                return "No se puede eliminar ni desactivar el gerente porque tiene facturas asociadas.";

            bool tieneRecordatorios = await _context.Recordatorios
                .AsNoTracking()
                .AnyAsync(r => r.IdUsuario == idUsuario);
            if (tieneRecordatorios)
                return "No se puede eliminar ni desactivar el gerente porque tiene recordatorios asociados.";

            bool tieneCajas = await _context.DiaCajas
                .AsNoTracking()
                .AnyAsync(c => c.IdUsuario == idUsuario || c.IdUsuarioCierre == idUsuario);
            if (tieneCajas)
                return "No se puede eliminar ni desactivar el gerente porque tiene información de caja asociada.";

            if (gerenteDb.IdSede.HasValue && gerenteDb.IdSede.Value > 0)
            {
                int sedeId = gerenteDb.IdSede.Value;

                bool tieneTesoreroOEmpleado = await _context.Usuarios
                    .AsNoTracking()
                    .AnyAsync(u =>
                        u.IdUsuario != idUsuario &&
                        u.IdSede == sedeId &&
                        u.IdRol != 1 &&
                        u.IdRol != 2);

                if (tieneTesoreroOEmpleado)
                    return "No se puede eliminar ni desactivar el gerente porque tiene un tesorero o empleado asociado en la sede.";
            }

            return null;
        }

        public async Task<string?> CambiarEstadoGerenteAsync(int idUsuario)
        {
            var usuario = await _context.Usuarios.FindAsync(idUsuario);
            if (usuario == null)
                return "El gerente no existe en el sistema.";

            if (usuario.EstadoUsuario)
            {
                string? motivoBloqueo = await ValidarDependenciasGerenteAsync(idUsuario);
                if (!string.IsNullOrEmpty(motivoBloqueo))
                    return motivoBloqueo;
            }

            usuario.EstadoUsuario = !usuario.EstadoUsuario;
            await _context.SaveChangesAsync();
            return null;
        }

        public async Task<List<GerenteDto>> ObtenerGerentesAsync()
        {
            var gerentes = await _context.Usuarios
                .AsNoTracking()
                .Where(u => u.IdRol == 2)
                .ToListAsync();

            var sedesDict = await _context.Sedes
                .AsNoTracking()
                .ToDictionaryAsync(s => s.IdSede, s => s.NomSede);

            return gerentes.Select(g => new GerenteDto
            {
                Id = g.IdUsuario,
                Nombres = g.NomUsuario ?? string.Empty,
                Apellidos = g.ApelliUsuario ?? string.Empty,
                TipoDocumento = g.TipDoc ? "CC" : "CE",
                NumeroDocumento = g.NumDoc.ToString(),
                Correo = g.Correo ?? string.Empty,
                Telefono = g.Telefono ?? string.Empty,
                Direccion = g.DirUsuario ?? string.Empty,
                IdSede = g.IdSede,
                Sede = g.IdSede.HasValue && sedesDict.ContainsKey(g.IdSede.Value) ? sedesDict[g.IdSede.Value] : string.Empty,
                FechaNacimiento = g.FechaNaci.ToDateTime(TimeOnly.MinValue)
            }).ToList();
        }

        public async Task<GerenteDto?> ObtenerGerentePorIdAsync(int id)
        {
            var g = await _context.Usuarios.FindAsync(id);
            if (g == null) return null;

            var nombreSede = g.IdSede.HasValue
                ? (await _context.Sedes
                    .AsNoTracking()
                    .Where(s => s.IdSede == g.IdSede.Value)
                    .Select(s => s.NomSede)
                    .FirstOrDefaultAsync() ?? string.Empty)
                : string.Empty;

            return new GerenteDto
            {
                Id = g.IdUsuario,
                Nombres = g.NomUsuario ?? string.Empty,
                Apellidos = g.ApelliUsuario ?? string.Empty,
                TipoDocumento = g.TipDoc ? "CC" : "CE",
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
            if (!gerente.IdSede.HasValue || gerente.IdSede.Value <= 0)
                return "La sede es obligatoria.";

            if (await _context.Usuarios.AnyAsync(u => u.NumDoc.ToString() == gerente.NumeroDocumento))
                return "El número de documento ya está registrado en el sistema.";

            if (await _context.Usuarios.AnyAsync(u => u.Correo == gerente.Correo))
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
                IdSede = gerente.IdSede.Value,
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
            if (!gerente.IdSede.HasValue || gerente.IdSede.Value <= 0)
                return "La sede es obligatoria.";

            var usuarioDb = await _context.Usuarios.FindAsync(gerente.Id);
            if (usuarioDb == null) return "El gerente no existe en la base de datos.";

            if (usuarioDb.EstadoUsuario == false && gerente.IdSede.Value != usuarioDb.IdSede)
            {
                return "No se puede reasignar o modificar un gerente inactivo con información asociada.";
            }

            if (await _context.Usuarios.AnyAsync(u => u.NumDoc.ToString() == gerente.NumeroDocumento && u.IdUsuario != gerente.Id))
                return "El número de documento ya pertenece a otro usuario.";

            if (await _context.Usuarios.AnyAsync(u => u.Correo == gerente.Correo && u.IdUsuario != gerente.Id))
                return "El correo electrónico ya pertenece a otro usuario.";

            usuarioDb.TipDoc = (gerente.TipoDocumento == "CC" || gerente.TipoDocumento == "1");
            usuarioDb.NumDoc = int.Parse(gerente.NumeroDocumento ?? "0");
            usuarioDb.NomUsuario = gerente.Nombres ?? string.Empty;
            usuarioDb.ApelliUsuario = gerente.Apellidos ?? string.Empty;
            usuarioDb.Correo = gerente.Correo ?? string.Empty;
            usuarioDb.Telefono = gerente.Telefono ?? string.Empty;
            usuarioDb.DirUsuario = gerente.Direccion ?? string.Empty;
            usuarioDb.IdSede = gerente.IdSede.Value;

            if (gerente.FechaNacimiento.HasValue)
                usuarioDb.FechaNaci = DateOnly.FromDateTime(gerente.FechaNacimiento.Value);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return "Error al actualizar en BD: " + (ex.InnerException?.Message ?? ex.Message);
            }

            return null;
        }

        public async Task<string?> EliminarGerenteAsync(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
                return "El gerente no existe en el sistema.";

            string? motivoBloqueo = await ValidarDependenciasGerenteAsync(id);
            if (!string.IsNullOrEmpty(motivoBloqueo))
                return motivoBloqueo;

            try
            {
                _context.Usuarios.Remove(usuario);
                await _context.SaveChangesAsync();
                return null;
            }
            catch (DbUpdateException ex)
            {
                return "No se puede eliminar el gerente porque tiene información relacionada en la base de datos: " + (ex.InnerException?.Message ?? ex.Message);
            }
        }
    }
}
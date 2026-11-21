using Microsoft.EntityFrameworkCore;
using Software_SARA.Dtos;
using Software_SARA.Repositories;
using Software_SARA.Repositories.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Software_SARA.Services
{
    public class SedesServices
    {
        private readonly SedeRepository _sedeRepository;
        private readonly SarabdContext _context;

        public SedesServices(SedeRepository sedeRepository, SarabdContext context)
        {
            _sedeRepository = sedeRepository;
            _context = context;
        }

        private async Task<string?> ValidarDependenciasSedeAsync(int idSede)
        {
            var idsUsuariosSede = await _context.Usuarios
                .Where(u => u.IdSede == idSede)
                .Select(u => u.IdUsuario)
                .ToListAsync();

            if (idsUsuariosSede.Count == 0)
                return null;

            bool tieneFacturas = await _context.Facturas
                .AnyAsync(f => idsUsuariosSede.Contains(f.IdUsuario));

            bool tieneRecordatorios = await _context.Recordatorios
                .AnyAsync(r => idsUsuariosSede.Contains(r.IdUsuario));

            bool tieneCajas = await _context.DiaCajas
                .AnyAsync(c => idsUsuariosSede.Contains(c.IdUsuario) ||
                              (c.IdUsuarioCierre.HasValue && idsUsuariosSede.Contains(c.IdUsuarioCierre.Value)));

            if (tieneFacturas || tieneRecordatorios || tieneCajas)
            {
                return "No se puede desactivar ni eliminar la sede porque tiene información operativa (facturas, recordatorios o caja) registrada.";
            }

            return null;
        }

        private static string NormalizarTexto(string? valor)
        {
            return (valor ?? string.Empty).Trim().ToLowerInvariant();
        }

        public async Task<SedeDto> CreateSede(SedeDto sedeDto)
        {
            sedeDto.EstadoSede = true;

            var listaSedesExistentes = await ObtenerTodasLasSedes();

            string nombreNuevo = NormalizarTexto(sedeDto.NomSede);
            string ciudadNueva = NormalizarTexto(sedeDto.CiuSede);
            string direccionNueva = NormalizarTexto(sedeDto.DirSede);

            bool nombreExiste = listaSedesExistentes.Any(s => NormalizarTexto(s.NomSede) == nombreNuevo);
            bool direccionEnMismaCiudadExiste = listaSedesExistentes.Any(s =>
                NormalizarTexto(s.CiuSede) == ciudadNueva &&
                NormalizarTexto(s.DirSede) == direccionNueva);

            if (nombreExiste)
            {
                sedeDto.Reponse = 0;
                sedeDto.Message = "Ya existe una sede registrada con ese nombre.";
                return sedeDto;
            }

            if (direccionEnMismaCiudadExiste)
            {
                sedeDto.Reponse = 0;
                sedeDto.Message = "Ya existe una sede registrada con esa dirección en la misma ciudad.";
                return sedeDto;
            }

            var result = await _sedeRepository.CreateSede(sedeDto);

            if (result)
            {
                sedeDto.Reponse = 1;
                sedeDto.Message = "Sede creada correctamente.";
            }
            else
            {
                sedeDto.Reponse = 0;
                sedeDto.Message = "No se pudo crear la sede.";
            }
            return sedeDto;
        }

        public async Task<List<SedeDto>> ObtenerTodasLasSedes()
        {
            var sedes = await _sedeRepository.ObtenerTodasLasSedes();

            return sedes.Select(sede => new SedeDto
            {
                IdSede = sede.IdSede,
                NomSede = sede.NomSede,
                DirSede = sede.DirSede,
                TelSede = sede.TelSede,
                EstadoSede = sede.EstadoSede,
                CiuSede = sede.CiuSede,
                DepSede = sede.DepSede,
                BarrioSede = sede.BarrioSede
            }).ToList();
        }

        public async Task<SedeDto> ObtenerSedePorId(int id)
        {
            var listaSedes = await ObtenerTodasLasSedes();
            return listaSedes.FirstOrDefault(s => s.IdSede == id);
        }

        public async Task<SedeDto> EditarSede(SedeDto sedeDto)
        {
            var listaSedesExistentes = await ObtenerTodasLasSedes();

            string nombreEditado = NormalizarTexto(sedeDto.NomSede);
            string ciudadEditada = NormalizarTexto(sedeDto.CiuSede);
            string direccionEditada = NormalizarTexto(sedeDto.DirSede);

            bool nombreConflicto = listaSedesExistentes.Any(s =>
                s.IdSede != sedeDto.IdSede &&
                NormalizarTexto(s.NomSede) == nombreEditado);

            bool direccionEnMismaCiudadConflicto = listaSedesExistentes.Any(s =>
                s.IdSede != sedeDto.IdSede &&
                NormalizarTexto(s.CiuSede) == ciudadEditada &&
                NormalizarTexto(s.DirSede) == direccionEditada);

            if (nombreConflicto)
            {
                sedeDto.Reponse = 0;
                sedeDto.Message = "El nombre de la sede ya pertenece a otra sede existente.";
                return sedeDto;
            }

            if (direccionEnMismaCiudadConflicto)
            {
                sedeDto.Reponse = 0;
                sedeDto.Message = "Ya existe otra sede con esa dirección en la misma ciudad.";
                return sedeDto;
            }

            var result = await _sedeRepository.EditarSede(sedeDto);
            if (result)
            {
                sedeDto.Reponse = 1;
                sedeDto.Message = "Sede editada correctamente.";
            }
            else
            {
                sedeDto.Reponse = 0;
                sedeDto.Message = "Error al editar la Sede.";
            }
            return sedeDto;
        }

        public async Task<SedeDto> EliminarSede(int idSede)
        {
            var sedeDto = new SedeDto { IdSede = idSede };

            var listaSedes = await ObtenerTodasLasSedes();
            var sedeAInactivar = listaSedes.FirstOrDefault(s => s.IdSede == idSede);

            if (sedeAInactivar == null)
            {
                sedeDto.Reponse = 0;
                sedeDto.Message = "La sede seleccionada no se encuentra registrada en el sistema.";
                return sedeDto;
            }

            if (!sedeAInactivar.EstadoSede)
            {
                sedeDto.Reponse = 0;
                sedeDto.Message = "La sede ya se encuentra inactiva y no puede ser procesada nuevamente.";
                return sedeDto;
            }

            string? motivoBloqueo = await ValidarDependenciasSedeAsync(idSede);
            if (!string.IsNullOrEmpty(motivoBloqueo))
            {
                sedeDto.Reponse = 0;
                sedeDto.Message = motivoBloqueo;
                return sedeDto;
            }

            var result = await _sedeRepository.EliminarSede(idSede);

            if (result)
            {
                sedeDto.Reponse = 1;
                sedeDto.Message = "Sede desactivada correctamente.";
            }
            else
            {
                sedeDto.Reponse = 0;
                sedeDto.Message = "Error al desactivar la Sede.";
            }
            return sedeDto;
        }

        public async Task<SedeDto> ReactivarSede(int idSede)
        {
            var sedeDto = new SedeDto { IdSede = idSede };
            var result = await _sedeRepository.ReactivarSede(idSede);

            if (result)
            {
                sedeDto.Reponse = 1;
                sedeDto.Message = "Sede reactivada correctamente.";
            }
            else
            {
                sedeDto.Reponse = 0;
                sedeDto.Message = "No se pudo reactivar la sede.";
            }

            return sedeDto;
        }
    }
}
using Software_SARA.Dtos;
using Software_SARA.Repositories;
using System.Collections.Generic;
using System.Linq;

namespace Software_SARA.Services
{
    public class SedesServices
    {
        private readonly SedeRepository _sedeRepository;
        
        public SedesServices(SedeRepository sedeRepository)
        {
            _sedeRepository = sedeRepository;
        }

        public async Task<SedeDto> CreateSede(SedeDto sedeDto)
        {
            sedeDto.EstadoSede = true; // Set EstadoSede to true (1) when creating a new Sede
            var result = await _sedeRepository.CreateSede(sedeDto);
            if (result)
            {
                sedeDto.Reponse = 1;
                sedeDto.Message = "Sede creada correctamente.";
            }
            else
            {
                sedeDto.Reponse = 0;
                sedeDto.Message = "La Sede ya existe.";
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
            // Obtiene la lista completa usando la función 
            var listaSedes = await ObtenerTodasLasSedes();

            // Retorna la sede que coincida con el ID
            return listaSedes.FirstOrDefault(s => s.IdSede == id);
        }

        public async Task<SedeDto> EditarSede(SedeDto sedeDto)
        {
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
            var result = await _sedeRepository.EliminarSede(idSede);
            if (result)
            {
                sedeDto.Reponse = 1;
                sedeDto.Message = "Sede eliminada correctamente.";
            }
            else
            {
                sedeDto.Reponse = 0;
                sedeDto.Message = "Error al eliminar la Sede.";
            }
            return sedeDto;
        }
    }
}

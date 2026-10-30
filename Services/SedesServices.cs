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
    }
}

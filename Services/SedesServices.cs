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

            // Obtenemos todas las sedes actuales para validar las reglas de negocio
            var listaSedesExistentes = await ObtenerTodasLasSedes();

            // RN02-CU03: Validar que el nombre de la sede y la dirección física sean únicos
            bool nombreExiste = listaSedesExistentes.Any(s => s.NomSede.Trim().ToLower() == sedeDto.NomSede.Trim().ToLower());
            bool direccionExiste = listaSedesExistentes.Any(s => s.DirSede.Trim().ToLower() == sedeDto.DirSede.Trim().ToLower());

            if (nombreExiste)
            {
                sedeDto.Reponse = 0;
                sedeDto.Message = "Ya existe una sede registrada con ese nombre.";
                return sedeDto;
            }

            if (direccionExiste)
            {
                sedeDto.Reponse = 0;
                sedeDto.Message = "Ya existe una sede registrada con esa dirección física.";
                return sedeDto;
            }

            // Si pasa las validaciones de negocio, pasa a registrar en el repositorio
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
            // Obtenemos la lista de todas las sedes para validar contra registros existentes
            var listaSedesExistentes = await ObtenerTodasLasSedes();

            // RN02-CU04: Si el Dueño cambia el nombre o la dirección, valida que no entre en conflicto con otra sede existente para ello, filtra descartando la sede que se está editando comparando el IdSede.
            bool nombreConflicto = listaSedesExistentes.Any(s => s.IdSede != sedeDto.IdSede && s.NomSede.Trim().ToLower() == sedeDto.NomSede.Trim().ToLower());

            bool direccionConflicto = listaSedesExistentes.Any(s => s.IdSede != sedeDto.IdSede && s.DirSede.Trim().ToLower() == sedeDto.DirSede.Trim().ToLower());

            if (nombreConflicto)
            {
                sedeDto.Reponse = 0;
                sedeDto.Message = "El nombre de la sede ya pertenece a otra sede existente.";
                return sedeDto;
            }

            if (direccionConflicto)
            {
                sedeDto.Reponse = 0;
                sedeDto.Message = "La dirección física ya pertenece a otra sede existente.";
                return sedeDto;
            }

            // Si pasa las validaciones de negocio, pasa a actualizar en el repositorio
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

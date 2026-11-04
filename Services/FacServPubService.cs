using Microsoft.AspNetCore.Mvc.Rendering;
using Software_SARA.Dtos;
using Software_SARA.Repositories;

namespace Software_SARA.Services
{
    public class FacServPubService
    {
        private readonly FacServPubRepository _facServPubRepository;

        public FacServPubService(FacServPubRepository facServPubRepository)
        {
            _facServPubRepository = facServPubRepository;
        }

        public async Task<FacServPublicDto> CreateFacturaServicioPublico(FacServPublicDto facServPublicDto)
        {
            facServPublicDto.EstadoFactura = true; // Set EstadoFactura to true (1) when creating a new FacturaServicioPublico
            var result = await _facServPubRepository.CreateFacturaServicioPublico(facServPublicDto);

            if (result)
            {
                facServPublicDto.Reponse = 1;
                facServPublicDto.Message = "Factura de servicio público creada correctamente.";
            }
            else
            {
                facServPublicDto.Reponse = 0;
                facServPublicDto.Message = "La factura de servicio público ya existe.";
            }
            return facServPublicDto;
        }

        public async Task<List<FacServPublicDto>> ObtenerTodasLasFacturasServiciosPublicos()
        {
            var facturas = await _facServPubRepository.ObtenerTodasLasFacturasServiciosPublicos();

            return facturas.Select(fac => new FacServPublicDto
            {
                IdFactura = fac.IdFactura,
                IdUsuario = fac.IdUsuario,
                TipFactura = fac.TipFactura,
                IdProveedor = fac.IdProveedor,
                IdServicio = fac.IdServicio,
                ValorFactura = fac.ValorFactura,
                FechaRegFactura = fac.FechaRegFactura,
                FechaVencimiento = fac.FechaVencimiento,
                EstadoFactura = fac.EstadoFactura
            }).ToList();
        }

        // Método que invoca la consulta SQL plana desde el repositorio
        public async Task<List<SelectListItem>> ObtenerServiciosPublicos()
        {
            return await _facServPubRepository.ObtenerListaServiciosPublicos();
        }

        public async Task<FacServPublicDto> ObtenerFacturaServicioPublicoPorId(int id)
        {
            // Obtiene la lista completa usando la función 
            var listaFacturas = await ObtenerTodasLasFacturasServiciosPublicos();

            // Retorna la factura de servicio público que coincida con el ID
            return listaFacturas.FirstOrDefault(f => f.IdFactura == id);
        }

        public async Task<FacServPublicDto> EditarFacturaServicioPublico(FacServPublicDto facServPublicDto)
        {
            var result = await _facServPubRepository.EditarFacturaServicioPublico(facServPublicDto);
            if (result)
            {
                facServPublicDto.Reponse = 1;
                facServPublicDto.Message = "Factura de servicio público editada correctamente.";
            }
            else
            {
                facServPublicDto.Reponse = 0;
                facServPublicDto.Message = "Error al editar la Factura de servicio público.";
            }
            return facServPublicDto;
        }

        public async Task<FacServPublicDto> ConfirmarPagoFacturaServicioPublico(int idFactura)
        {
            var facServPublicDto = new FacServPublicDto { IdFactura = idFactura };
            var result = await _facServPubRepository.ConfirmarPagoFacturaServicioPublico(idFactura);

            if (result)
            {
                facServPublicDto.Reponse = 1;
                facServPublicDto.Message = "Pago de factura de servicio público confirmado correctamente.";
            }
            else
            {
                facServPublicDto.Reponse = 0;
                facServPublicDto.Message = "Error al confirmar el pago de la factura de servicio público.";
            }
            return facServPublicDto;
        }
    }
}

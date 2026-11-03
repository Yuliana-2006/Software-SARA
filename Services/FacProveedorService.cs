using Microsoft.AspNetCore.Mvc.Rendering;
using Software_SARA.Dtos;
using Software_SARA.Repositories;

namespace Software_SARA.Services
{
    public class FacProveedorService
    {
        private readonly FacProveedorRepository _facProveedorRepository;

        public FacProveedorService(FacProveedorRepository facProveedorRepository)
        {
            _facProveedorRepository = facProveedorRepository;
        }

        public async Task<FacProveedorDto> CreateFacturaProveedor(FacProveedorDto facProveedorDto)
        {
            facProveedorDto.EstadoFactura = true; // Set EstadoFactura to true (1) when creating a new FacturaProveedor
            var result = await _facProveedorRepository.CreateFacturaProveedor(facProveedorDto);

            if (result)
            {
                facProveedorDto.Reponse = 1;
                facProveedorDto.Message = "Factura de proveedor creada correctamente.";
            }
            else
            {
                facProveedorDto.Reponse = 0;
                facProveedorDto.Message = "La factura de proveedor ya existe.";
            }
            return facProveedorDto;
        }

        public async Task<List<FacProveedorDto>> ObtenerTodasLasFacturasProveedores()
        {
            var facturas = await _facProveedorRepository.ObtenerTodasLasFacturasProveedores();

            return facturas.Select(fac => new FacProveedorDto
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
        public async Task<List<SelectListItem>> ObtenerProveedores()
        {
            return await _facProveedorRepository.ObtenerListaProveedores();
        }

        public async Task<FacProveedorDto> ObtenerFacturaProveedorPorId(int id)
        {
            // Obtiene la lista completa usando la función 
            var listaFacturas = await ObtenerTodasLasFacturasProveedores();

            // Retorna la factura de proveedor que coincida con el ID
            return listaFacturas.FirstOrDefault(f => f.IdFactura == id);
        }

        public async Task<FacProveedorDto> EditarFacturaProveedor(FacProveedorDto facProveedorDto)
        {
            var result = await _facProveedorRepository.EditarFacturaProveedor(facProveedorDto);
            if (result)
            {
                facProveedorDto.Reponse = 1;
                facProveedorDto.Message = "Factura de proveedor editada correctamente.";
            }
            else
            {
                facProveedorDto.Reponse = 0;
                facProveedorDto.Message = "Error al editar la Factura de proveedor.";
            }
            return facProveedorDto;
        }

        public async Task<FacProveedorDto> ConfirmarPagoFacturaProveedor(int idFactura)
        {
            var facProveedorDto = new FacProveedorDto { IdFactura = idFactura };
            var result = await _facProveedorRepository.ConfirmarPagoFacturaProveedor(idFactura);

            if (result)
            {
                facProveedorDto.Reponse = 1;
                facProveedorDto.Message = "Pago de factura de proveedor confirmado correctamente.";
            }
            else
            {
                facProveedorDto.Reponse = 0;
                facProveedorDto.Message = "Error al confirmar el pago de la factura de proveedor.";
            }
            return facProveedorDto;
        }
    }
}

using Microsoft.AspNetCore.Mvc.Rendering;
using Software_SARA.Dtos;
using Software_SARA.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            facProveedorDto.EstadoFactura = true;
            facProveedorDto.FechaRegFactura = DateOnly.FromDateTime(DateTime.Now);

            if (facProveedorDto.IdProveedor == null || facProveedorDto.IdProveedor <= 0)
            {
                facProveedorDto.Reponse = 0;
                facProveedorDto.Message = "Debe seleccionar un proveedor.";
                return facProveedorDto;
            }

            if (facProveedorDto.ValorFactura <= 10000m)
            {
                facProveedorDto.Reponse = 0;
                facProveedorDto.Message = "Ingrese un valor mayor a $10,000 para la factura.";
                return facProveedorDto;
            }

            if (facProveedorDto.FechaVencimiento < facProveedorDto.FechaRegFactura)
            {
                facProveedorDto.Reponse = 0;
                facProveedorDto.Message = "La fecha de vencimiento no puede ser inferior a la fecha de registro.";
                return facProveedorDto;
            }

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
                NomProveedor = fac.NomProveedor,
                IdServicio = fac.IdServicio,
                ValorFactura = fac.ValorFactura,
                FechaRegFactura = fac.FechaRegFactura,
                FechaVencimiento = fac.FechaVencimiento,
                EstadoFactura = fac.EstadoFactura
            }).ToList();
        }

        public async Task<List<SelectListItem>> ObtenerProveedores()
        {
            return await _facProveedorRepository.ObtenerListaProveedores();
        }

        public async Task<FacProveedorDto?> ObtenerFacturaProveedorPorId(int id)
        {
            var listaFacturas = await ObtenerTodasLasFacturasProveedores();
            return listaFacturas.FirstOrDefault(f => f.IdFactura == id);
        }

        public async Task<FacProveedorDto> EditarFacturaProveedor(FacProveedorDto facProveedorDto)
        {
            var facturaActual = await ObtenerFacturaProveedorPorId(facProveedorDto.IdFactura);
            if (facturaActual == null)
            {
                facProveedorDto.Reponse = 0;
                facProveedorDto.Message = "La factura de proveedor no existe.";
                return facProveedorDto;
            }

            if (facturaActual.EstadoFactura == false)
            {
                facProveedorDto.EstadoFactura = false;
                facProveedorDto.Reponse = 0;
                facProveedorDto.Message = "No se puede editar la factura porque su pago ya fue confirmado.";
                return facProveedorDto;
            }

            facProveedorDto.EstadoFactura = facturaActual.EstadoFactura;
            if (facProveedorDto.FechaRegFactura == default)
            {
                facProveedorDto.FechaRegFactura = facturaActual.FechaRegFactura;
            }

            if (facProveedorDto.IdProveedor == null || facProveedorDto.IdProveedor <= 0)
            {
                facProveedorDto.Reponse = 0;
                facProveedorDto.Message = "Debe seleccionar un proveedor.";
                return facProveedorDto;
            }

            if (facProveedorDto.ValorFactura <= 10000m)
            {
                facProveedorDto.Reponse = 0;
                facProveedorDto.Message = "Ingrese un valor mayor a $10,000 para la factura.";
                return facProveedorDto;
            }

            if (facProveedorDto.FechaVencimiento < facProveedorDto.FechaRegFactura)
            {
                facProveedorDto.Reponse = 0;
                facProveedorDto.Message = "La fecha de vencimiento no puede ser inferior a la fecha de registro.";
                return facProveedorDto;
            }

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

            var facturaExistente = await ObtenerFacturaProveedorPorId(idFactura);
            if (facturaExistente != null && facturaExistente.EstadoFactura == false)
            {
                facProveedorDto.Reponse = 0;
                facProveedorDto.Message = "El pago de esta factura ya fue confirmado anteriormente.";
                return facProveedorDto;
            }

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

        public async Task<List<FacProveedorDto>> ObtenerHistorialFacturasProveedoresAsync()
        {
            return await _facProveedorRepository.ObtenerHistorialFacturasProveedoresAsync();
        }

        public async Task<List<FacProveedorDto>> ObtenerVencimientosProximosProveedores()
        {
            var todasLasFacturas = await ObtenerTodasLasFacturasProveedores();

            var hoy = DateOnly.FromDateTime(DateTime.Now);
            var limite = hoy.AddDays(7);

            return todasLasFacturas
                .Where(f => f.EstadoFactura && f.IdProveedor != null && f.IdProveedor > 0 && f.FechaVencimiento >= hoy && f.FechaVencimiento <= limite)
                .ToList();
        }
    }
}
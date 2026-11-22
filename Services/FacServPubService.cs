using Microsoft.AspNetCore.Mvc.Rendering;
using Software_SARA.Dtos;
using Software_SARA.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Software_SARA.Services
{
    public class FacServPubService
    {
        private readonly FacServPubRepository _facServPubRepository;

        public FacServPubService(FacServPubRepository facServPubRepository)
        {
            _facServPubRepository = facServPubRepository;
        }

        public virtual async Task<FacServPublicDto> CreateFacturaServicioPublico(FacServPublicDto facServPublicDto)
        {
            facServPublicDto.EstadoFactura = true;
            facServPublicDto.FechaRegFactura = DateOnly.FromDateTime(DateTime.Now);

            if (facServPublicDto.IdServicio == null || facServPublicDto.IdServicio <= 0)
            {
                facServPublicDto.Reponse = 0;
                facServPublicDto.Message = "Debe seleccionar un servicio público.";
                return facServPublicDto;
            }

            if (facServPublicDto.ValorFactura <= 10000m)
            {
                facServPublicDto.Reponse = 0;
                facServPublicDto.Message = "Ingrese un valor mayor a $10,000 para la factura.";
                return facServPublicDto;
            }

            if (facServPublicDto.FechaVencimiento < facServPublicDto.FechaRegFactura)
            {
                facServPublicDto.Reponse = 0;
                facServPublicDto.Message = "La fecha de vencimiento no puede ser inferior a la fecha de registro.";
                return facServPublicDto;
            }

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

        public virtual async Task<List<FacServPublicDto>> ObtenerTodasLasFacturasServiciosPublicos()
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

        public virtual async Task<List<FacServPublicDto>> ObtenerVencimientosProximos()
        {
            var fechaActual = DateOnly.FromDateTime(DateTime.Now);
            var fechaLimite = fechaActual.AddDays(10);

            var todasLasFacturas = await ObtenerTodasLasFacturasServiciosPublicos();

            return todasLasFacturas
                .Where(f => f.EstadoFactura && f.FechaVencimiento >= fechaActual && f.FechaVencimiento <= fechaLimite)
                .ToList();
        }

        public virtual async Task<List<SelectListItem>> ObtenerServiciosPublicos()
        {
            return await _facServPubRepository.ObtenerListaServiciosPublicos();
        }

        public virtual async Task<FacServPublicDto?> ObtenerFacturaServicioPublicoPorId(int id)
        {
            var listaFacturas = await ObtenerTodasLasFacturasServiciosPublicos();
            return listaFacturas.FirstOrDefault(f => f.IdFactura == id);
        }

        public virtual async Task<FacServPublicDto> EditarFacturaServicioPublico(FacServPublicDto facServPublicDto)
        {
            if (facServPublicDto.IdServicio == null || facServPublicDto.IdServicio <= 0)
            {
                facServPublicDto.Reponse = 0;
                facServPublicDto.Message = "Debe seleccionar un servicio público.";
                return facServPublicDto;
            }

            if (facServPublicDto.ValorFactura <= 10000m)
            {
                facServPublicDto.Reponse = 0;
                facServPublicDto.Message = "Ingrese un valor mayor a $10,000 para la factura.";
                return facServPublicDto;
            }

            if (facServPublicDto.FechaVencimiento < facServPublicDto.FechaRegFactura)
            {
                facServPublicDto.Reponse = 0;
                facServPublicDto.Message = "La fecha de vencimiento no puede ser inferior a la fecha de registro.";
                return facServPublicDto;
            }

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

        public virtual async Task<FacServPublicDto> ConfirmarPagoFacturaServicioPublico(int idFactura)
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

        public virtual async Task<List<FacServPublicDto>> ObtenerHistorialFacturasServiciosPublicosAsync()
        {
            return await _facServPubRepository.ObtenerHistorialFacturasServiciosPublicosAsync();
        }
    }
}
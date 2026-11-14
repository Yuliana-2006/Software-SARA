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

        public async Task<FacServPublicDto> CreateFacturaServicioPublico(FacServPublicDto facServPublicDto)
        {
            facServPublicDto.EstadoFactura = true;

            if (facServPublicDto.IdServicio == null || facServPublicDto.IdServicio <= 0)
            {
                facServPublicDto.Reponse = 0;
                facServPublicDto.Message = "Debe seleccionar un servicio público.";
                return facServPublicDto;
            }

            if (facServPublicDto.ValorFactura < 20000)
            {
                facServPublicDto.Reponse = 0;
                facServPublicDto.Message = "Ingrese un valor mayor o igual a $20,000 para la factura.";
                return facServPublicDto;
            }

            // CORRECCIÓN
            var hoy = DateOnly.FromDateTime(DateTime.Now);
            if (facServPublicDto.FechaRegFactura < hoy)
            {
                facServPublicDto.Reponse = 0;
                facServPublicDto.Message = "La fecha de registro no puede ser una fecha anterior a la actual.";
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

        public async Task<List<FacServPublicDto>> ObtenerVencimientosProximos()
        {
            // CORRECCIÓN
            var fechaActual = DateOnly.FromDateTime(DateTime.Now);
            var fechaLimite = fechaActual.AddDays(10);

            var todasLasFacturas = await ObtenerTodasLasFacturasServiciosPublicos();

            var vencimientosProximos = todasLasFacturas
                .Where(f => f.FechaVencimiento >= fechaActual && f.FechaVencimiento <= fechaLimite)
                .ToList();

            return vencimientosProximos;
        }

        public async Task<List<SelectListItem>> ObtenerServiciosPublicos()
        {
            return await _facServPubRepository.ObtenerListaServiciosPublicos();
        }

        public async Task<FacServPublicDto> ObtenerFacturaServicioPublicoPorId(int id)
        {
            var listaFacturas = await ObtenerTodasLasFacturasServiciosPublicos();
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
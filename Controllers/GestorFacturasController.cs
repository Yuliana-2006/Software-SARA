using Microsoft.AspNetCore.Mvc;
using Software_SARA.Dtos;
using Software_SARA.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Software_SARA.Controllers
{
    public class GestorFacturasController : Controller
    {
        private readonly FacProveedorService _facProveedorService;
        private readonly FacServPubService _facServPubService;

        public GestorFacturasController(
            FacProveedorService facProveedorService,
            FacServPubService facServPubService)
        {
            _facProveedorService = facProveedorService;
            _facServPubService = facServPubService;
        }

        [HttpGet]
        public IActionResult SelectFactura()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> HistorialFacturas()
        {
            var historialProveedores = await _facProveedorService.ObtenerHistorialFacturasProveedoresAsync()
                ?? new List<FacProveedorDto>();

            var historialServicios = await _facServPubService.ObtenerHistorialFacturasServiciosPublicosAsync()
                ?? new List<FacServPublicDto>();

            var listaFinal = new List<FacProveedorDto>();

            foreach (var p in historialProveedores)
            {
                p.TipFactura = false;
                p.Message = "Proveedor";
                listaFinal.Add(p);
            }

            foreach (var s in historialServicios)
            {
                listaFinal.Add(new FacProveedorDto
                {
                    IdFactura = s.IdFactura,
                    IdUsuario = s.IdUsuario,
                    NomUsuario = s.NomUsuario,
                    TipFactura = true,
                    IdProveedor = s.IdProveedor,
                    NomProveedor = !string.IsNullOrWhiteSpace(s.NomServicio) ? s.NomServicio : "Servicio Público",
                    IdServicio = s.IdServicio,
                    ValorFactura = s.ValorFactura,
                    FechaRegFactura = s.FechaRegFactura,
                    FechaVencimiento = s.FechaVencimiento,
                    FechaPago = s.FechaPago,
                    EstadoFactura = s.EstadoFactura,
                    Message = "ServicioPublico"
                });
            }

            var ordenados = listaFinal.OrderByDescending(x => x.IdFactura).ToList();
            return View(ordenados);
        }
    }
}
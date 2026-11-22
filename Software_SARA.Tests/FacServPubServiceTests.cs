using Microsoft.AspNetCore.Mvc.Rendering;
using Software_SARA.Dtos;
using Software_SARA.Repositories;
using Software_SARA.Repositories.Models;
using Software_SARA.Services;
using Xunit;

namespace Software_SARA.Tests
{
    public class FakeFacServPubRepository : FacServPubRepository
    {
        private readonly List<Factura> _facturas;
        private readonly bool _resultado;

        public FakeFacServPubRepository(List<Factura>? facturas = null, bool resultado = true)
            : base(null!, null!)
        {
            _facturas = facturas ?? new List<Factura>();
            _resultado = resultado;
        }

        public override Task<List<Factura>> ObtenerTodasLasFacturasServiciosPublicos()
            => Task.FromResult(_facturas);

        public override Task<bool> CreateFacturaServicioPublico(FacServPublicDto dto)
            => Task.FromResult(_resultado);

        public override Task<bool> EditarFacturaServicioPublico(FacServPublicDto dto)
            => Task.FromResult(_resultado);

        public override Task<bool> ConfirmarPagoFacturaServicioPublico(int idFactura)
            => Task.FromResult(_resultado);

        public override Task<List<SelectListItem>> ObtenerListaServiciosPublicos()
            => Task.FromResult(new List<SelectListItem> { new SelectListItem { Value = "1", Text = "Agua" } });

        public override Task<List<FacServPublicDto>> ObtenerHistorialFacturasServiciosPublicosAsync()
            => Task.FromResult(new List<FacServPublicDto>());
    }

    public class FacServPubServiceTests
    {
        [Fact]
        public async Task CreateFacturaServicioPublico_SinServicio_RetornaError()
        {
            var service = new FacServPubService(new FakeFacServPubRepository());
            var dto = new FacServPublicDto
            {
                IdServicio = 0,
                ValorFactura = 25000m,
                FechaVencimiento = DateOnly.FromDateTime(DateTime.Now.AddDays(5))
            };

            var res = await service.CreateFacturaServicioPublico(dto);

            Assert.Equal(0, res.Reponse);
            Assert.Equal("Debe seleccionar un servicio público.", res.Message);
        }

        [Fact]
        public async Task CreateFacturaServicioPublico_ValorMenorOIgualDiezMil_RetornaError()
        {
            var service = new FacServPubService(new FakeFacServPubRepository());
            var dto = new FacServPublicDto
            {
                IdServicio = 1,
                ValorFactura = 10000m,
                FechaVencimiento = DateOnly.FromDateTime(DateTime.Now.AddDays(5))
            };

            var res = await service.CreateFacturaServicioPublico(dto);

            Assert.Equal(0, res.Reponse);
            Assert.Equal("Ingrese un valor mayor a $10,000 para la factura.", res.Message);
        }

        [Fact]
        public async Task CreateFacturaServicioPublico_DatosValidos_CreaFactura()
        {
            var service = new FacServPubService(new FakeFacServPubRepository(resultado: true));
            var dto = new FacServPublicDto
            {
                IdServicio = 2,
                ValorFactura = 85000m,
                FechaVencimiento = DateOnly.FromDateTime(DateTime.Now.AddDays(10))
            };

            var res = await service.CreateFacturaServicioPublico(dto);

            Assert.Equal(1, res.Reponse);
            Assert.True(res.EstadoFactura);
            Assert.Equal("Factura de servicio público creada correctamente.", res.Message);
        }

        [Fact]
        public async Task ObtenerVencimientosProximos_FiltraSoloActivasDentroDeDiezDias()
        {
            var hoy = DateOnly.FromDateTime(DateTime.Now);
            var facturas = new List<Factura>
            {
                new Factura { IdFactura = 1, EstadoFactura = true, FechaVencimiento = hoy.AddDays(3), ValorFactura = 30000m },
                new Factura { IdFactura = 2, EstadoFactura = true, FechaVencimiento = hoy.AddDays(15), ValorFactura = 40000m },
                new Factura { IdFactura = 3, EstadoFactura = false, FechaVencimiento = hoy.AddDays(2), ValorFactura = 50000m }
            };

            var service = new FacServPubService(new FakeFacServPubRepository(facturas));
            var proximos = await service.ObtenerVencimientosProximos();

            Assert.Single(proximos);
            Assert.Equal(1, proximos[0].IdFactura);
        }
    }
}
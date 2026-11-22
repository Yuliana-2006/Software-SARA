using Software_SARA.Dtos;
using Software_SARA.Repositories;
using Software_SARA.Repositories.Models;
using Software_SARA.Services;
using Xunit;

namespace Software_SARA.Tests
{
    public class FakeFacProveedorRepository : FacProveedorRepository
    {
        private readonly List<Factura> _facturasExistentes;
        private readonly bool _retornoOperacion;

        public FakeFacProveedorRepository(List<Factura>? facturasExistentes = null, bool retornoOperacion = true)
            : base(null!, null!)
        {
            _facturasExistentes = facturasExistentes ?? new List<Factura>();
            _retornoOperacion = retornoOperacion;
        }

        public override Task<List<Factura>> ObtenerTodasLasFacturasProveedores()
        {
            return Task.FromResult(_facturasExistentes);
        }

        public override Task<bool> CreateFacturaProveedor(FacProveedorDto facProveedorDto)
        {
            return Task.FromResult(_retornoOperacion);
        }

        public override Task<bool> EditarFacturaProveedor(FacProveedorDto facProveedorDto)
        {
            return Task.FromResult(_retornoOperacion);
        }

        public override Task<List<FacProveedorDto>> ObtenerHistorialFacturasProveedoresAsync()
        {
            return Task.FromResult(new List<FacProveedorDto>());
        }
    }

    public class FacProveedorServiceTests
    {
        [Fact]
        public async Task CreateFacturaProveedor_SinProveedorSeleccionado_RetornaError()
        {
            // Arrange
            var fakeRepo = new FakeFacProveedorRepository();
            var service = new FacProveedorService(fakeRepo);
            var hoy = DateOnly.FromDateTime(DateTime.Today);

            var dto = new FacProveedorDto
            {
                IdProveedor = 0,
                ValorFactura = 50000,
                FechaRegFactura = hoy,
                FechaVencimiento = hoy.AddDays(7)
            };

            // Act
            var resultado = await service.CreateFacturaProveedor(dto);

            // Assert
            Assert.Equal(0, resultado.Reponse);
            Assert.Equal("Debe seleccionar un proveedor.", resultado.Message);
        }

        [Fact]
        public async Task CreateFacturaProveedor_DatosValidos_CreaFacturaCorrectamente()
        {
            // Arrange
            var fakeRepo = new FakeFacProveedorRepository(retornoOperacion: true);
            var service = new FacProveedorService(fakeRepo);
            var hoy = DateOnly.FromDateTime(DateTime.Today);

            var dto = new FacProveedorDto
            {
                IdUsuario = 1,
                TipFactura = true,
                IdProveedor = 21,
                ValorFactura = 450000,
                FechaRegFactura = hoy,
                FechaVencimiento = hoy.AddDays(15)
            };

            // Act
            var resultado = await service.CreateFacturaProveedor(dto);

            // Assert
            Assert.True(resultado.EstadoFactura);
            Assert.Equal(1, resultado.Reponse);
            Assert.Equal("Factura de proveedor creada correctamente.", resultado.Message);
        }

        [Fact]
        public async Task EditarFacturaProveedor_FacturaInactiva_BloqueaEdicion()
        {
            // Arrange: simulamos que la factura #7 ya está inactiva (EstadoFactura = false)
            var hoy = DateOnly.FromDateTime(DateTime.Today);
            var facturasSimuladas = new List<Factura>
            {
                new Factura
                {
                    IdFactura = 7,
                    IdProveedor = 21,
                    ValorFactura = 450000,
                    FechaRegFactura = hoy,
                    FechaVencimiento = hoy.AddDays(10),
                    EstadoFactura = false
                }
            };

            var fakeRepo = new FakeFacProveedorRepository(facturasExistentes: facturasSimuladas);
            var service = new FacProveedorService(fakeRepo);

            var dtoEditar = new FacProveedorDto
            {
                IdFactura = 7,
                IdProveedor = 21,
                ValorFactura = 500000,
                FechaRegFactura = hoy,
                FechaVencimiento = hoy.AddDays(12)
            };

            // Act
            var resultado = await service.EditarFacturaProveedor(dtoEditar);

            // Assert
            Assert.Equal(0, resultado.Reponse);
            Assert.Equal("No se puede editar la factura porque su pago ya fue confirmado.", resultado.Message);
        }
    }
}
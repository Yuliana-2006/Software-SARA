using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Software_SARA.Controllers;
using Software_SARA.Dtos;
using Software_SARA.Repositories.Models;
using Software_SARA.Services;
using Xunit;

namespace Software_SARA.Tests
{
    public class FacturasControllersTests
    {
        private static void PrepararControlador(Controller controller, string? usuarioIdEnSesion = "1")
        {
            var httpContext = new DefaultHttpContext
            {
                Session = new TestSession()
            };
            if (!string.IsNullOrEmpty(usuarioIdEnSesion))
            {
                httpContext.Session.SetString("UsuarioId", usuarioIdEnSesion);
            }

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            controller.TempData = new TempDataDictionary(httpContext, new DummyTempDataProvider());
        }

        [Fact]
        public async Task ProveedoresController_CrearFacturaPost_SinSesion_RedirigeALogin()
        {
            var service = new FacProveedorService(new FakeFacProveedorRepository());
            var controller = new ProveedoresController(service);
            PrepararControlador(controller, usuarioIdEnSesion: null);

            var dto = new FacProveedorDto
            {
                IdProveedor = 1,
                ValorFactura = 50000m,
                FechaVencimiento = DateOnly.FromDateTime(DateTime.Now.AddDays(5))
            };

            var result = await controller.CrearFacturaProveedor(dto);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirect.ActionName);
            Assert.Equal("Acceso", redirect.ControllerName);
        }

        [Fact]
        public async Task ProveedoresController_CrearFacturaPost_DatosValidos_RedirigeALista()
        {
            var service = new FacProveedorService(new FakeFacProveedorRepository(retornoOperacion: true));
            var controller = new ProveedoresController(service);
            PrepararControlador(controller, usuarioIdEnSesion: "3");

            var dto = new FacProveedorDto
            {
                IdProveedor = 2,
                ValorFactura = 120000m,
                FechaVencimiento = DateOnly.FromDateTime(DateTime.Now.AddDays(7))
            };

            var result = await controller.CrearFacturaProveedor(dto);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(ProveedoresController.FacturasProveedores), redirect.ActionName);
            Assert.Equal(3, dto.IdUsuario);
        }

        [Fact]
        public async Task ProveedoresController_EditarFacturaGet_Inexistente_RetornaNotFound()
        {
            var service = new FacProveedorService(new FakeFacProveedorRepository(new List<Factura>()));
            var controller = new ProveedoresController(service);
            PrepararControlador(controller);

            var result = await controller.EditarFacturaProveedor(999);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task ServiciosPublicosController_FacturasServiciosPublicos_RetornaViewConLista()
        {
            var facturas = new List<Factura>
            {
                new Factura { IdFactura = 10, IdServicio = 1, ValorFactura = 65000m, EstadoFactura = true }
            };
            var service = new FacServPubService(new FakeFacServPubRepository(facturas, resultado: true));
            var controller = new ServiciosPublicosController(service);
            PrepararControlador(controller);

            var result = await controller.FacturasServiciosPublicos();

            var viewResult = Assert.IsType<ViewResult>(result);
            var modelo = Assert.IsAssignableFrom<List<FacServPublicDto>>(viewResult.Model);
            Assert.Single(modelo);
        }

        [Fact]
        public async Task ServiciosPublicosController_CrearFacturaPost_ValorInvalido_AgregaErrorModelStateYRetornaVista()
        {
            var service = new FacServPubService(new FakeFacServPubRepository(resultado: true));
            var controller = new ServiciosPublicosController(service);
            PrepararControlador(controller, usuarioIdEnSesion: "2");

            var dto = new FacServPublicDto
            {
                IdServicio = 1,
                ValorFactura = 5000m, // <= 10000m
                FechaVencimiento = DateOnly.FromDateTime(DateTime.Now.AddDays(3))
            };

            var result = await controller.CrearFacturaServicioPublico(dto);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.Equal(dto, viewResult.Model);
        }
    }

    internal class DummyTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object?> LoadTempData(HttpContext context) => new Dictionary<string, object?>();
        public void SaveTempData(HttpContext context, IDictionary<string, object?> values) { }
    }
}
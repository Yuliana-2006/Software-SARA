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
    public class ServiciosPublicosControllerTests
    {
        private static ServiciosPublicosController CrearControlador(
            List<Factura>? facturasExistentes = null,
            bool retornoOperacion = true,
            string? usuarioId = "10")
        {
            var session = new TestSession();
            if (!string.IsNullOrEmpty(usuarioId))
                session.SetString("UsuarioId", usuarioId);

            var httpContext = new DefaultHttpContext { Session = session };
            var repo = new FakeFacServPubRepository(facturasExistentes ?? new List<Factura>(), retornoOperacion);
            var service = new FacServPubService(repo);

            return new ServiciosPublicosController(service)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext },
                TempData = new TempDataDictionary(httpContext, new DummyTempDataProvider())
            };
        }

        private static FacServPublicDto CrearDtoValido(int idFactura = 0, int idServicio = 1, bool estadoFactura = true)
        {
            var hoy = DateOnly.FromDateTime(DateTime.Today);
            return new FacServPublicDto
            {
                IdFactura = idFactura,
                IdUsuario = 10,
                TipFactura = false,
                IdServicio = idServicio,
                ValorFactura = 85000m,
                FechaRegFactura = hoy,
                FechaVencimiento = hoy.AddDays(10),
                EstadoFactura = estadoFactura
            };
        }

        [Fact]
        public async Task FacturasServiciosPublicos_SinUsuarioEnSesion_RetornaVista()
        {
            var controller = CrearControlador(usuarioId: null);

            var result = await controller.FacturasServiciosPublicos();

            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task FacturasServiciosPublicos_ConUsuarioEnSesion_RetornaVistaConLista()
        {
            var facturas = new List<Factura>
            {
                new Factura
                {
                    IdFactura = 1,
                    IdUsuario = 10,
                    IdServicio = 1,
                    ValorFactura = 60000m,
                    FechaRegFactura = DateOnly.FromDateTime(DateTime.Today),
                    FechaVencimiento = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
                    EstadoFactura = true
                }
            };

            var controller = CrearControlador(facturasExistentes: facturas, usuarioId: "10");

            var result = await controller.FacturasServiciosPublicos();

            var view = Assert.IsType<ViewResult>(result);
            var modelo = Assert.IsAssignableFrom<List<FacServPublicDto>>(view.Model);
            Assert.Single(modelo);
        }

        [Fact]
        public async Task CrearFacturaServicioPublicoGet_RetornaVista()
        {
            var controller = CrearControlador(usuarioId: "10");

            var result = await controller.CrearFacturaServicioPublico();

            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task CrearFacturaServicioPublicoPost_Valido_RedirigeAFacturasServiciosPublicos()
        {
            var controller = CrearControlador(retornoOperacion: true, usuarioId: "10");
            var dto = CrearDtoValido();

            var result = await controller.CrearFacturaServicioPublico(dto);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(ServiciosPublicosController.FacturasServiciosPublicos), redirect.ActionName);
            Assert.NotNull(controller.TempData["MensajeExito"]);
        }

        [Fact]
        public async Task CrearFacturaServicioPublicoPost_ValorInvalidoYFechaVencimientoAnterior_RetornaVistaConErrores()
        {
            var controller = CrearControlador(usuarioId: "10");

            var hoy = DateOnly.FromDateTime(DateTime.Today);
            var dto = new FacServPublicDto
            {
                IdUsuario = 10,
                IdServicio = 1,
                ValorFactura = 5000m,
                FechaRegFactura = hoy,
                FechaVencimiento = hoy.AddDays(-2),
                EstadoFactura = true
            };

            var result = await controller.CrearFacturaServicioPublico(dto);

            var view = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
        }

        [Fact]
        public async Task EditarFacturaServicioPublicoGet_InexistenteRetornaNotFound_YExistenteRetornaVista()
        {
            var facturas = new List<Factura>
            {
                new Factura
                {
                    IdFactura = 20,
                    IdUsuario = 10,
                    IdServicio = 2,
                    ValorFactura = 90000m,
                    FechaRegFactura = DateOnly.FromDateTime(DateTime.Today),
                    FechaVencimiento = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
                    EstadoFactura = true
                }
            };

            var controller = CrearControlador(facturasExistentes: facturas, usuarioId: "10");

            var resNoExiste = await controller.EditarFacturaServicioPublico(999);
            Assert.IsType<NotFoundResult>(resNoExiste);

            var resOk = await controller.EditarFacturaServicioPublico(20);
            var viewOk = Assert.IsType<ViewResult>(resOk);
            var dto = Assert.IsType<FacServPublicDto>(viewOk.Model);
            Assert.Equal(20, dto.IdFactura);
        }

        [Fact]
        public async Task EditarFacturaServicioPublicoPost_ValidoYConErrores_CubreRamas()
        {
            var hoy = DateOnly.FromDateTime(DateTime.Today);
            var facturas = new List<Factura>
            {
                new Factura
                {
                    IdFactura = 20,
                    IdUsuario = 10,
                    IdServicio = 2,
                    ValorFactura = 90000m,
                    FechaRegFactura = hoy,
                    FechaVencimiento = hoy.AddDays(7),
                    EstadoFactura = true
                }
            };

            var controller = CrearControlador(facturasExistentes: facturas, retornoOperacion: true, usuarioId: "10");

            var dtoValido = CrearDtoValido(idFactura: 20, idServicio: 2, estadoFactura: true);
            var resOk = await controller.EditarFacturaServicioPublico(dtoValido);
            var redirectOk = Assert.IsType<RedirectToActionResult>(resOk);
            Assert.Equal(nameof(ServiciosPublicosController.FacturasServiciosPublicos), redirectOk.ActionName);
            Assert.NotNull(controller.TempData["MensajeExito"]);

            var dtoInvalido = CrearDtoValido(idFactura: 20, idServicio: 0, estadoFactura: true);
            var resErr = await controller.EditarFacturaServicioPublico(dtoInvalido);
            var viewErr = Assert.IsType<ViewResult>(resErr);
            Assert.False(controller.ModelState.IsValid);
        }

        [Fact]
        public async Task ConfirmarPagoFacturaServicioPublicoGetYPost_CubreRamas()
        {
            var hoy = DateOnly.FromDateTime(DateTime.Today);
            var facturas = new List<Factura>
            {
                new Factura
                {
                    IdFactura = 30,
                    IdUsuario = 10,
                    IdServicio = 1,
                    ValorFactura = 120000m,
                    FechaRegFactura = hoy,
                    FechaVencimiento = hoy.AddDays(4),
                    EstadoFactura = true
                }
            };

            var controller = CrearControlador(facturasExistentes: facturas, retornoOperacion: true, usuarioId: "10");

            var resGetNoExiste = await controller.ConfirmarPagoFacturaServicioPublico(999);
            Assert.IsType<NotFoundResult>(resGetNoExiste);

            var resGetOk = await controller.ConfirmarPagoFacturaServicioPublico(30);
            var viewConfirmar = Assert.IsType<ViewResult>(resGetOk);
            var dtoConfirmar = Assert.IsType<FacServPublicDto>(viewConfirmar.Model);
            Assert.Equal(30, dtoConfirmar.IdFactura);

            var resPostOk = await controller.ConfirmarPagoFacturaServicioPublico(new FacServPublicDto { IdFactura = 30 });
            var redirectPostOk = Assert.IsType<RedirectToActionResult>(resPostOk);
            Assert.Equal(nameof(ServiciosPublicosController.FacturasServiciosPublicos), redirectPostOk.ActionName);
            Assert.NotNull(controller.TempData["MensajeExito"]);
        }

        [Fact]
        public async Task VencimientoProximos_SinUsuarioYConUsuario_RetornanVista()
        {
            var controllerSinSesion = CrearControlador(usuarioId: null);
            var resSinSesion = await controllerSinSesion.VencimientoProximos();
            Assert.IsType<ViewResult>(resSinSesion);

            var facturas = new List<Factura>
            {
                new Factura
                {
                    IdFactura = 40,
                    IdUsuario = 10,
                    IdServicio = 1,
                    ValorFactura = 55000m,
                    FechaRegFactura = DateOnly.FromDateTime(DateTime.Today),
                    FechaVencimiento = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
                    EstadoFactura = true
                }
            };
            var controllerConSesion = CrearControlador(facturasExistentes: facturas, usuarioId: "10");
            var resVista = await controllerConSesion.VencimientoProximos();
            var view = Assert.IsType<ViewResult>(resVista);
            var lista = Assert.IsAssignableFrom<List<FacServPublicDto>>(view.Model);
            Assert.Single(lista);
        }
    }
}
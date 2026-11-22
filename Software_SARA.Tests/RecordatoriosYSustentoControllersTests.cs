using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Software_SARA.Controllers;
using Software_SARA.Dtos;
using Software_SARA.Services;
using Xunit;

namespace Software_SARA.Tests
{
    public class RecordatoriosYSustentoControllersTests
    {
        private static void PrepararControlador(Controller controller, string? rol = "Gerente", string? usuarioId = "1", string? sedeId = "1")
        {
            var httpContext = new DefaultHttpContext
            {
                Session = new TestSession()
            };
            if (!string.IsNullOrEmpty(rol)) httpContext.Session.SetString("UsuarioRol", rol);
            if (!string.IsNullOrEmpty(usuarioId)) httpContext.Session.SetString("UsuarioId", usuarioId);
            if (!string.IsNullOrEmpty(sedeId)) httpContext.Session.SetString("SedeId", sedeId);

            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new TempDataDictionary(httpContext, new DummyTempDataProvider());
        }

        [Fact]
        public async Task RecordatoriosController_Index_ComoTesorero_RedirigeAEntregasProximas()
        {
            var httpAccessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext { Session = new TestSession() } };
            var service = new RecordatorioPedidoService(new FakeRecordatorioPedidoRepository(), httpAccessor);
            var controller = new RecordatoriosController(service);
            PrepararControlador(controller, rol: "Tesorero");

            var result = await controller.Index();

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(RecordatoriosController.EntregasProximas), redirect.ActionName);
        }

        [Fact]
        public async Task RecordatoriosController_CrearGet_ComoTesorero_BloqueaYRedirige()
        {
            var httpAccessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext { Session = new TestSession() } };
            var service = new RecordatorioPedidoService(new FakeRecordatorioPedidoRepository(), httpAccessor);
            var controller = new RecordatoriosController(service);
            PrepararControlador(controller, rol: "Tesorero");

            var result = await controller.Crear();

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(RecordatoriosController.EntregasProximas), redirect.ActionName);
            Assert.NotNull(controller.TempData["MensajeError"]);
        }

        [Fact]
        public async Task RecordatoriosController_ConfirmarPost_ActualizaEstadoYRedirige()
        {
            var httpAccessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext { Session = new TestSession() } };
            var service = new RecordatorioPedidoService(new FakeRecordatorioPedidoRepository(resultado: true), httpAccessor);
            var controller = new RecordatoriosController(service);
            PrepararControlador(controller, rol: "Gerente");

            var result = await controller.ConfirmarPost(5);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(RecordatoriosController.Index), redirect.ActionName);
            Assert.Equal("Entrega del pedido confirmada exitosamente.", controller.TempData["MensajeExito"]);
        }

        [Fact]
        public void SustentoController_Index_RetornaVista()
        {
            var config = new ConfigurationBuilder().Build();
            var controller = new SustentoController(config);
            PrepararControlador(controller, rol: "Tesorero");

            var result = controller.Index();

            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void SustentoController_Calcular_SinCadenaConexionValida_CapturaErrorYRedirigeAIndex()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=NoExiste;Trusted_Connection=True;Connection Timeout=1;"
                })
                .Build();

            var controller = new SustentoController(config);
            PrepararControlador(controller, rol: "Tesorero", sedeId: "1");

            var result = controller.Calcular(DateTime.Today);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(SustentoController.Index), redirect.ActionName);
            Assert.NotNull(controller.TempData["MensajeError"]);
        }
    }
}
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Software_SARA.Controllers;
using Software_SARA.Dtos;
using Software_SARA.Repositories.Models;
using Software_SARA.Services;
using Xunit;

namespace Software_SARA.Tests
{
    public class CajaYGestorFacturasControllersTests
    {
        private static SarabdContext CrearContexto(string dbName)
        {
            var options = new DbContextOptionsBuilder<SarabdContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new SarabdContext(options);
        }

        private static void Preparar(Controller controller, string? usuarioId = null)
        {
            var session = new TestSession();
            if (!string.IsNullOrEmpty(usuarioId))
                session.SetString("UsuarioId", usuarioId);

            var httpContext = new DefaultHttpContext { Session = session };
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new TempDataDictionary(httpContext, new DummyTempDataProvider());
        }

        [Fact]
        public void CajaController_Index_RetornaVista()
        {
            using var context = CrearContexto(nameof(CajaController_Index_RetornaVista));
            var controller = new CajaController(context);
            Preparar(controller, "1");

            Assert.IsType<ViewResult>(controller.Index());
        }

        [Fact]
        public void CajaController_AperturaGet_SinUsuarioEnSesion_RedirigeALogin()
        {
            using var context = CrearContexto(nameof(CajaController_AperturaGet_SinUsuarioEnSesion_RedirigeALogin));
            var controller = new CajaController(context);
            Preparar(controller, usuarioId: null);

            var result = controller.Apertura();

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirect.ActionName);
            Assert.Equal("Acceso", redirect.ControllerName);
        }

        [Fact]
        public async Task CajaController_AperturaGet_ConCajaAbiertaEnSede_BloqueaYRedirigeAIndex()
        {
            using var context = CrearContexto(nameof(CajaController_AperturaGet_ConCajaAbiertaEnSede_BloqueaYRedirigeAIndex));
            context.Usuarios.Add(new Usuario
            {
                IdUsuario = 1,
                IdSede = 2,
                NomUsuario = "Carlos",
                ApelliUsuario = "Ruiz",
                Correo = "carlos@test.com",
                DirUsuario = "Calle 10",
                Telefono = "3005551122",
                EstadoUsuario = true
            });
            context.DiaCajas.Add(new DiaCaja
            {
                IdDiaCaja = 1,
                IdUsuario = 1,
                EstadoDia = true,
                MontoInicial = 100000m,
                FechaHoraApertura = DateTime.Now
            });
            await context.SaveChangesAsync();

            var controller = new CajaController(context);
            Preparar(controller, usuarioId: "1");

            var result = controller.Apertura();

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.NotNull(controller.TempData["MensajeError"]);
        }

        [Fact]
        public async Task CajaController_CierreGet_SinCajaAbierta_RedirigeAIndexConError()
        {
            using var context = CrearContexto(nameof(CajaController_CierreGet_SinCajaAbierta_RedirigeAIndexConError));
            context.Usuarios.Add(new Usuario
            {
                IdUsuario = 4,
                IdSede = 1,
                NomUsuario = "Laura",
                ApelliUsuario = "Mora",
                Correo = "laura@test.com",
                DirUsuario = "Cra 5",
                Telefono = "3104442211",
                EstadoUsuario = true
            });
            await context.SaveChangesAsync();

            var controller = new CajaController(context);
            Preparar(controller, usuarioId: "4");

            var result = controller.Cierre();

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.NotNull(controller.TempData["MensajeError"]);
        }

        [Fact]
        public async Task GestorFacturasController_SelectFacturaYHistorialFacturas_RetornanVistas()
        {
            var provService = new FacProveedorService(new FakeFacProveedorRepository(new List<Factura>()));
            var servService = new FacServPubService(new FakeFacServPubRepository(new List<Factura>()));
            var controller = new GestorFacturasController(provService, servService);
            Preparar(controller, "1");

            Assert.IsType<ViewResult>(controller.SelectFactura());

            var historialResult = await controller.HistorialFacturas();
            var view = Assert.IsType<ViewResult>(historialResult);
            var lista = Assert.IsAssignableFrom<List<FacProveedorDto>>(view.Model);
            Assert.Empty(lista);
        }
    }
}
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Software_SARA.Controllers;
using Software_SARA.Models;
using Software_SARA.Repositories.Models;
using Xunit;

namespace Software_SARA.Tests
{
    public class AccesoErrorHomeControllersTests
    {
        private static SarabdContext CrearContexto(string dbName)
        {
            var options = new DbContextOptionsBuilder<SarabdContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new SarabdContext(options);
        }

        private static void AdjuntarSesion(Controller controller, Action<ISession>? configurar = null)
        {
            var session = new TestSession();
            configurar?.Invoke(session);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { Session = session }
            };
        }

        [Fact]
        public void AccesoController_LoginGetYAccesoDenegado_RetornanVista()
        {
            using var context = CrearContexto(nameof(AccesoController_LoginGetYAccesoDenegado_RetornanVista));
            var controller = new AccesoController(context);

            Assert.IsType<ViewResult>(controller.Login());
            Assert.IsType<ViewResult>(controller.AccesoDenegado());
        }

        [Fact]
        public void AccesoController_LoginPost_ModelStateInvalido_RetornaVistaConModelo()
        {
            using var context = CrearContexto(nameof(AccesoController_LoginPost_ModelStateInvalido_RetornaVistaConModelo));
            var controller = new AccesoController(context);
            controller.ModelState.AddModelError("NumeroDocumento", "Requerido");
            var modelo = new LoginViewModel();

            var result = controller.Login(modelo);

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal(modelo, view.Model);
        }

        [Fact]
        public void AccesoController_LoginPost_UsuarioInexistente_AsignaErrorEnViewBag()
        {
            using var context = CrearContexto(nameof(AccesoController_LoginPost_UsuarioInexistente_AsignaErrorEnViewBag));
            var controller = new AccesoController(context);
            AdjuntarSesion(controller);

            var modelo = new LoginViewModel
            {
                TipoDocumento = "CC",
                NumeroDocumento = "1000999",
                Contrasena = "123"
            };

            var result = controller.Login(modelo);

            Assert.IsType<ViewResult>(result);
            Assert.Equal("El documento ingresado no existe.", controller.ViewBag.Error);
        }

        [Fact]
        public void AccesoController_CerrarSesion_LimpiaSesionYRedirigeALogin()
        {
            using var context = CrearContexto(nameof(AccesoController_CerrarSesion_LimpiaSesionYRedirigeALogin));
            var controller = new AccesoController(context);
            AdjuntarSesion(controller, s => s.SetString("UsuarioId", "10"));

            var result = controller.CerrarSesion();

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirect.ActionName);
            Assert.Null(controller.HttpContext.Session.GetString("UsuarioId"));
        }

        [Fact]
        public void ErrorController_IndexYStatusCode_RetornanVistaConMensaje()
        {
            var controller = new ErrorController();

            Assert.IsType<ViewResult>(controller.Index());

            var res404 = controller.StatusCode(404);
            Assert.IsType<ViewResult>(res404);
            Assert.Equal(404, controller.ViewBag.CodigoError);
            Assert.Equal("La página que buscas no existe o fue movida (Error de Host).", (string)controller.ViewBag.Mensaje);

            var res500 = controller.StatusCode(500);
            Assert.IsType<ViewResult>(res500);
            Assert.Equal("Ocurrió un problema inesperado en el sistema.", controller.ViewBag.Mensaje);
        }

        [Fact]
        public void HomeController_IndexPrivacyYGenerarHash_RetornanResultadosEsperados()
        {
            var controller = new HomeController();
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            Assert.IsType<ViewResult>(controller.Index());
            Assert.IsType<ViewResult>(controller.Privacy());
            Assert.IsType<ViewResult>(controller.Error());

            var contentResult = Assert.IsType<ContentResult>(controller.GenerarHash());
            Assert.False(string.IsNullOrWhiteSpace(contentResult.Content));
        }
    }
}
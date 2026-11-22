using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Software_SARA.Controllers;
using Software_SARA.Dtos;
using Software_SARA.Repositories.Models;
using Software_SARA.Services;
using Xunit;

namespace Software_SARA.Tests
{
    public class SedesYPersonalControllersTests
    {
        private static SarabdContext CrearContexto(string dbName)
        {
            var options = new DbContextOptionsBuilder<SarabdContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new SarabdContext(options);
        }

        private static void PrepararControlador(Controller controller, string? usuarioId = null, string? sedeId = null)
        {
            var session = new TestSession();
            if (usuarioId != null) session.SetString("UsuarioId", usuarioId);
            if (sedeId != null) session.SetString("SedeId", sedeId);

            var httpContext = new DefaultHttpContext { Session = session };
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new TempDataDictionary(httpContext, new DummyTempDataProvider());
        }

        [Fact]
        public async Task SedesController_RegistrarSede_LimpiaSedeDuenoYRetornaLista()
        {
            using var context = CrearContexto(nameof(SedesController_RegistrarSede_LimpiaSedeDuenoYRetornaLista));
            context.Usuarios.Add(new Usuario
            {
                IdUsuario = 1,
                NomUsuario = "Dueño",
                ApelliUsuario = "Demo",
                Correo = "dueno@test.com",
                DirUsuario = "Cra 1",
                Telefono = "3001112233",
                IdRol = 1,
                IdSede = 5,
                EstadoUsuario = true
            });
            await context.SaveChangesAsync();

            var sedesFake = new List<Sede>
            {
                new Sede { IdSede = 5, NomSede = "Sede Norte", EstadoSede = true }
            };
            var sedesService = new SedesServices(new FakeSedeRepository(sedesFake), context);
            var controller = new SedesController(sedesService, context);
            PrepararControlador(controller, usuarioId: "1", sedeId: "5");

            var result = await controller.RegistrarSede();

            var viewResult = Assert.IsType<ViewResult>(result);
            var lista = Assert.IsAssignableFrom<List<SedeDto>>(viewResult.Model);
            Assert.Single(lista);

            var duenoActualizado = await context.Usuarios.FindAsync(1);
            Assert.Null(duenoActualizado!.IdSede);
            Assert.Null(controller.HttpContext.Session.GetString("SedeId"));
        }

        [Fact]
        public async Task SedesController_EditarSedeGet_IdInexistente_RetornaNotFound()
        {
            using var context = CrearContexto(nameof(SedesController_EditarSedeGet_IdInexistente_RetornaNotFound));
            var sedesService = new SedesServices(new FakeSedeRepository(new List<Sede>()), context);
            var controller = new SedesController(sedesService, context);
            PrepararControlador(controller, usuarioId: "1");

            var result = await controller.EditarSede(404);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void PersonalController_Index_RetornaVista()
        {
            using var context = CrearContexto(nameof(PersonalController_Index_RetornaVista));
            var emailService = new EmailService(new ConfigurationBuilder().Build());
            var controller = new PersonalController(context, emailService);
            PrepararControlador(controller);

            var result = controller.Index();

            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void PersonalController_SeleccionarRol_SinGerenteEnSesion_RedirigeALogin()
        {
            using var context = CrearContexto(nameof(PersonalController_SeleccionarRol_SinGerenteEnSesion_RedirigeALogin));
            var emailService = new EmailService(new ConfigurationBuilder().Build());
            var controller = new PersonalController(context, emailService);
            PrepararControlador(controller, usuarioId: null);

            var result = controller.SeleccionarRol(3);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirect.ActionName);
            Assert.Equal("Acceso", redirect.ControllerName);
        }

        [Fact]
        public async Task PersonalController_Eliminar_UsuarioInexistente_RedirigeConMensajeError()
        {
            using var context = CrearContexto(nameof(PersonalController_Eliminar_UsuarioInexistente_RedirigeConMensajeError));
            var emailService = new EmailService(new ConfigurationBuilder().Build());
            var controller = new PersonalController(context, emailService);
            PrepararControlador(controller, usuarioId: "1");

            var result = await controller.Eliminar(999);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(PersonalController.SeleccionarRol), redirect.ActionName);
            Assert.Equal("El usuario seleccionado no existe.", controller.TempData["MensajeError"]);
        }
    }
}
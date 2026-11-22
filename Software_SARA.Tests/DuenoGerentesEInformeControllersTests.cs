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
    public class DuenoGerentesEInformeControllersTests
    {
        private static SarabdContext CrearContexto(string dbName)
        {
            var options = new DbContextOptionsBuilder<SarabdContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new SarabdContext(options);
        }

        private static void Preparar(Controller controller, string? sedeId = "1")
        {
            var session = new TestSession();
            if (!string.IsNullOrEmpty(sedeId))
                session.SetString("SedeId", sedeId);

            var httpContext = new DefaultHttpContext { Session = session };
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new TempDataDictionary(httpContext, new DummyTempDataProvider());
        }

        [Fact]
        public async Task DuenoController_IndexYCrearGet_RetornanVistas()
        {
            using var context = CrearContexto(nameof(DuenoController_IndexYCrearGet_RetornanVistas));
            var emailService = new EmailService(new ConfigurationBuilder().Build());
            var controller = new DuenoController(context, emailService);
            Preparar(controller);

            var indexRes = await controller.Index();
            Assert.IsType<ViewResult>(indexRes);
            Assert.IsType<ViewResult>(controller.Crear());
        }

        [Fact]
        public async Task DuenoController_CrearPost_MenorDeEdad_RetornaVistaConError()
        {
            using var context = CrearContexto(nameof(DuenoController_CrearPost_MenorDeEdad_RetornaVistaConError));
            var emailService = new EmailService(new ConfigurationBuilder().Build());
            var controller = new DuenoController(context, emailService);
            Preparar(controller);

            var dto = new DuenoDTO
            {
                TipoDoc = "CC",
                NumDoc = "102030",
                NomUsuario = "Juan",
                ApelliUsuario = "Díaz",
                Correo = "juan@test.com",
                Telefono = "3001234567",
                DirUsuario = "Cra 8",
                FechaNaci = DateOnly.FromDateTime(DateTime.Today.AddYears(-15))
            };

            var result = await controller.Crear(dto);

            Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
        }

        [Fact]
        public async Task GerentesController_IndexCrearGetYEliminarConfirmadoIdInvalido_FuncionanComoSeEspera()
        {
            using var context = CrearContexto(nameof(GerentesController_IndexCrearGetYEliminarConfirmadoIdInvalido_FuncionanComoSeEspera));
            var emailService = new EmailService(new ConfigurationBuilder().Build());
            var gerenteService = new GerenteService(context, emailService);
            var controller = new GerentesController(gerenteService, emailService, context);
            Preparar(controller);

            Assert.IsType<ViewResult>(await controller.Index());
            Assert.IsType<ViewResult>(controller.Crear());

            var elimRes = await controller.EliminarConfirmado(0, null);
            var redirect = Assert.IsType<RedirectToActionResult>(elimRes);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("El identificador del gerente no es válido.", controller.TempData["MensajeError"]);
        }

        [Fact]
        public async Task InformeController_IndexGenerarFechaFuturaYEnviarCorreoSinDestino_FuncionanComoSeEspera()
        {
            using var context = CrearContexto(nameof(InformeController_IndexGenerarFechaFuturaYEnviarCorreoSinDestino_FuncionanComoSeEspera));
            var emailService = new EmailService(new ConfigurationBuilder().Build());
            var controller = new InformeController(context, emailService);
            Preparar(controller, sedeId: "1");

            Assert.IsType<ViewResult>(controller.Index());

            var resFutura = controller.Generar(DateTime.Today.AddDays(1));
            var redirectFutura = Assert.IsType<RedirectToActionResult>(resFutura);
            Assert.Equal("Index", redirectFutura.ActionName);
            Assert.Equal("No se puede generar un informe para una fecha futura.", controller.TempData["MensajeError"]);

            var resSinCorreo = await controller.EnviarCorreo(DateTime.Today, "");
            var redirectSinCorreo = Assert.IsType<RedirectToActionResult>(resSinCorreo);
            Assert.Equal("Preview", redirectSinCorreo.ActionName);
            Assert.Equal("Por favor, seleccione un destinatario de la lista.", controller.TempData["MensajeError"]);
        }
    }
}
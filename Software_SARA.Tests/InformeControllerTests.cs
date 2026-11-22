using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QuestPDF.Infrastructure;
using Software_SARA.Controllers;
using Software_SARA.Dtos;
using Software_SARA.Repositories.Models;
using Software_SARA.Services;
using Xunit;

namespace Software_SARA.Tests
{
    public class InformeControllerTests
    {
        private static SarabdContext CrearContexto(string nombreDb)
        {
            var options = new DbContextOptionsBuilder<SarabdContext>()
                .UseInMemoryDatabase(databaseName: nombreDb)
                .Options;
            return new SarabdContext(options);
        }

        private static InformeController CrearControlador(
            SarabdContext context,
            string? sedeId = "1",
            string? usuarioId = null,
            string? rolUsuario = null,
            string? correoUsuario = null)
        {
            var session = new TestSession();
            if (!string.IsNullOrEmpty(sedeId)) session.SetString("SedeId", sedeId);
            if (!string.IsNullOrEmpty(usuarioId)) session.SetString("UsuarioId", usuarioId);
            if (!string.IsNullOrEmpty(rolUsuario)) session.SetString("RolUsuario", rolUsuario);
            if (!string.IsNullOrEmpty(correoUsuario)) session.SetString("CorreoUsuario", correoUsuario);

            var httpContext = new DefaultHttpContext { Session = session };
            var emailService = new EmailService(new ConfigurationBuilder().Build());

            return new InformeController(context, emailService)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext },
                TempData = new TempDataDictionary(httpContext, new DummyTempDataProvider())
            };
        }

        private static async Task PoblarDatosBaseAsync(SarabdContext context, DateTime fecha)
        {
            var fechaSolo = DateOnly.FromDateTime(fecha);

            context.Sedes.Add(new Sede
            {
                IdSede = 1,
                NomSede = "Sede Centro",
                BarrioSede = "Centro",
                CiuSede = "Madrid",
                DepSede = "Cundinamarca",
                DirSede = "Calle 10 # 5-20",
                TelSede = "3105556677",
                EstadoSede = true
            });

            context.Usuarios.AddRange(
                new Usuario
                {
                    IdUsuario = 10,
                    IdRol = 2,
                    IdSede = 1,
                    NomUsuario = "Laura",
                    ApelliUsuario = "Mora",
                    Correo = "laura@gmail.com",
                    Telefono = "3101112233",
                    DirUsuario = "Cra 5",
                    EstadoUsuario = true
                },
                new Usuario
                {
                    IdUsuario = 11,
                    IdRol = 4,
                    IdSede = 1,
                    NomUsuario = "Pedro",
                    ApelliUsuario = "Soler",
                    Correo = "pedro@gmail.com",
                    Telefono = "3104445566",
                    DirUsuario = "Cra 6",
                    EstadoUsuario = true
                }
            );

            context.Proveedors.Add(new Proveedor
            {
                IdProveedor = 1,
                NomProveedor = "Alquería"
            });

            context.Servicios.Add(new Servicio
            {
                IdServicio = 2,
                NomServicio = "Energía"
            });

            context.DiaCajas.Add(new DiaCaja
            {
                IdDiaCaja = 1,
                IdUsuario = 10,
                MontoInicial = 100000m,
                MontoFinal = 250000m,
                FechaHoraApertura = fecha.Date.AddHours(8),
                FechaHoraCierre = fecha.Date.AddHours(17),
                EstadoDia = false
            });

            context.Facturas.AddRange(
                new Factura
                {
                    IdFactura = 101,
                    IdUsuario = 10,
                    TipFactura = true,
                    IdProveedor = 1,
                    ValorFactura = 80000m,
                    FechaRegFactura = fechaSolo,
                    FechaVencimiento = fechaSolo,
                    EstadoFactura = false
                },
                new Factura
                {
                    IdFactura = 102,
                    IdUsuario = 10,
                    TipFactura = false,
                    IdServicio = 2,
                    ValorFactura = 45000m,
                    FechaRegFactura = fechaSolo,
                    FechaVencimiento = fechaSolo,
                    EstadoFactura = true
                }
            );

            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task GenerarYPreview_ConRegistrosEnFecha_RetornanVistaPreviewConTotales()
        {
            using var context = CrearContexto(nameof(GenerarYPreview_ConRegistrosEnFecha_RetornanVistaPreviewConTotales));
            var fecha = DateTime.Today;
            await PoblarDatosBaseAsync(context, fecha);

            var controller = CrearControlador(context, sedeId: "1", usuarioId: "10", rolUsuario: "Gerente", correoUsuario: "laura@gmail.com");

            var resGenerar = controller.Generar(fecha);
            var viewGenerar = Assert.IsType<ViewResult>(resGenerar);
            Assert.Equal("Preview", viewGenerar.ViewName);
            var dtoGenerado = Assert.IsType<InformeCajaDTO>(viewGenerar.Model);
            Assert.Equal("Sede Centro", dtoGenerado.NombreSede);
            Assert.Equal(100000m, dtoGenerado.TotalMontoInicial);
            Assert.Equal(80000m, dtoGenerado.TotalFacturasPendientes);
            Assert.Equal(45000m, dtoGenerado.TotalFacturasPagadas);

            var resPreview = controller.Preview(fecha);
            var viewPreview = Assert.IsType<ViewResult>(resPreview);
            Assert.IsType<InformeCajaDTO>(viewPreview.Model);
        }

        [Fact]
        public void GenerarYPreview_SinRegistrosEnFecha_RedirigenAIndexConError()
        {
            using var context = CrearContexto(nameof(GenerarYPreview_SinRegistrosEnFecha_RedirigenAIndexConError));
            var controller = CrearControlador(context, sedeId: "1");

            var resGenerar = controller.Generar(DateTime.Today.AddDays(-2));
            var redirectGenerar = Assert.IsType<RedirectToActionResult>(resGenerar);
            Assert.Equal("Index", redirectGenerar.ActionName);
            Assert.Equal("No se encontraron registros para la fecha seleccionada.", controller.TempData["MensajeError"]);

            var resPreview = controller.Preview(DateTime.Today.AddDays(-2));
            var redirectPreview = Assert.IsType<RedirectToActionResult>(resPreview);
            Assert.Equal("Index", redirectPreview.ActionName);
        }

        [Fact]
        public async Task DescargarPDF_ConRegistrosValidos_RetornaArchivoPdf()
        {
            QuestPDF.Settings.License = LicenseType.Community;

            using var context = CrearContexto(nameof(DescargarPDF_ConRegistrosValidos_RetornaArchivoPdf));
            var fecha = DateTime.Today;
            await PoblarDatosBaseAsync(context, fecha);

            var controller = CrearControlador(context, sedeId: "1", usuarioId: "10", rolUsuario: "Gerente");

            var result = controller.DescargarPDF(fecha);

            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("application/pdf", fileResult.ContentType);
            Assert.NotEmpty(fileResult.FileContents);
        }

        [Fact]
        public async Task EnviarCorreo_DestinatarioNoAutorizado_RedirigeAPreviewConError()
        {
            using var context = CrearContexto(nameof(EnviarCorreo_DestinatarioNoAutorizado_RedirigeAPreviewConError));
            var fecha = DateTime.Today;
            await PoblarDatosBaseAsync(context, fecha);

            var controller = CrearControlador(context, sedeId: "1", usuarioId: "10", rolUsuario: "Gerente");

            var result = await controller.EnviarCorreo(fecha, "pedro@gmail.com");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Preview", redirect.ActionName);
            Assert.Contains("usuario autorizado", controller.TempData["MensajeError"]?.ToString());
        }

        [Fact]
        public async Task EnviarCorreo_DestinatarioAutorizadoSinSmtpConfigurado_CapturaExcepcionYRedirigeAPreview()
        {
            using var context = CrearContexto(nameof(EnviarCorreo_DestinatarioAutorizadoSinSmtpConfigurado_CapturaExcepcionYRedirigeAPreview));
            var fecha = DateTime.Today;
            await PoblarDatosBaseAsync(context, fecha);

            var controller = CrearControlador(context, sedeId: "1", usuarioId: "10", rolUsuario: "Gerente");

            var result = await controller.EnviarCorreo(fecha, "laura@gmail.com");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Preview", redirect.ActionName);
            Assert.NotNull(controller.TempData["MensajeError"]);
        }
        
        
    }
}
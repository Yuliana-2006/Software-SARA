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
    public class CoberturaAdicionalTests
    {
        private static SarabdContext CrearContexto(string nombreDb)
        {
            var options = new DbContextOptionsBuilder<SarabdContext>()
                .UseInMemoryDatabase(databaseName: nombreDb)
                .Options;
            return new SarabdContext(options);
        }

        [Fact]
        public async Task SedesServices_EditarYReactivarSede_CubreRamasExitoYConflicto()
        {
            using var context = CrearContexto(nameof(SedesServices_EditarYReactivarSede_CubreRamasExitoYConflicto));
            var sedes = new List<Sede>
            {
                new Sede { IdSede = 1, NomSede = "Sede Norte", CiuSede = "Madrid", DirSede = "Calle 1", EstadoSede = true },
                new Sede { IdSede = 2, NomSede = "Sede Sur", CiuSede = "Facatativá", DirSede = "Carrera 2", EstadoSede = false }
            };

            var service = new SedesServices(new FakeSedeRepository(sedes, resultado: true), context);

            var conflicto = await service.EditarSede(new SedeDto
            {
                IdSede = 2,
                NomSede = "sede norte",
                CiuSede = "Facatativá",
                DirSede = "Carrera 2"
            });
            Assert.Equal(0, conflicto.Reponse);

            var editada = await service.EditarSede(new SedeDto
            {
                IdSede = 2,
                NomSede = "Sede Sur Actualizada",
                CiuSede = "Facatativá",
                DirSede = "Carrera 2"
            });
            Assert.Equal(1, editada.Reponse);

            var reactivada = await service.ReactivarSede(2);
            Assert.Equal(1, reactivada.Reponse);
            Assert.Equal("Sede reactivada correctamente.", reactivada.Message);
        }

        [Fact]
        public async Task GerenteService_CambiarEstadoYValidarYActualizar_CubreRamasPrincipales()
        {
            using var context = CrearContexto(nameof(GerenteService_CambiarEstadoYValidarYActualizar_CubreRamasPrincipales));
            context.Usuarios.Add(new Usuario
            {
                IdUsuario = 20,
                IdRol = 2,
                IdSede = 1,
                NomUsuario = "Luis",
                ApelliUsuario = "Cano",
                Correo = "luis@test.com",
                DirUsuario = "Calle 10",
                Telefono = "3001112233",
                NumDoc = 102030,
                TipDoc = true,
                FechaNaci = new DateOnly(2000, 1, 1),
                EstadoUsuario = true,
                Contraseña = "hash"
            });
            await context.SaveChangesAsync();

            var emailService = new EmailService(new ConfigurationBuilder().Build());
            var service = new GerenteService(context, emailService);

            var errorEstado = await service.CambiarEstadoGerenteAsync(20);
            Assert.Null(errorEstado);

            var usuarioActualizado = await context.Usuarios.FindAsync(20);
            Assert.False(usuarioActualizado!.EstadoUsuario);

            var errorInactivo = await service.ValidarYActualizarGerenteAsync(new GerenteDto
            {
                Id = 20,
                IdSede = 2,
                NumeroDocumento = "102030",
                Correo = "luis@test.com",
                Nombres = "Luis",
                Apellidos = "Cano"
            });
            Assert.Equal("No se puede reasignar o modificar un gerente inactivo con información asociada.", errorInactivo);
        }

        [Fact]
        public async Task FacProveedorService_EditarFacturaPagadaYConfirmarPago_CubreCasosBorde()
        {
            var hoy = DateOnly.FromDateTime(DateTime.Now);
            var facturas = new List<Factura>
            {
                new Factura
                {
                    IdFactura = 1,
                    IdProveedor = 1,
                    ValorFactura = 50000m,
                    FechaRegFactura = hoy,
                    FechaVencimiento = hoy.AddDays(5),
                    EstadoFactura = false
                },
                new Factura
                {
                    IdFactura = 2,
                    IdProveedor = 1,
                    ValorFactura = 75000m,
                    FechaRegFactura = hoy,
                    FechaVencimiento = hoy.AddDays(5),
                    EstadoFactura = true
                }
            };

            var service = new FacProveedorService(new FakeFacProveedorRepository(facturas, retornoOperacion: true));

            var editPagada = await service.EditarFacturaProveedor(new FacProveedorDto
            {
                IdFactura = 1,
                IdProveedor = 1,
                ValorFactura = 60000m,
                FechaVencimiento = hoy.AddDays(5)
            });
            Assert.Equal(0, editPagada.Reponse);
            Assert.Equal("No se puede editar la factura porque su pago ya fue confirmado.", editPagada.Message);

            var confirmarYaPagada = await service.ConfirmarPagoFacturaProveedor(1);
            Assert.Equal(0, confirmarYaPagada.Reponse);
            Assert.Equal("El pago de esta factura ya fue confirmado anteriormente.", confirmarYaPagada.Message);
        }

        [Fact]
        public void CajaController_AperturaPost_ModeloValidoRegistraCajaYRedirige()
        {
            using var context = CrearContexto(nameof(CajaController_AperturaPost_ModeloValidoRegistraCajaYRedirige));
            context.Usuarios.Add(new Usuario
            {
                IdUsuario = 7,
                IdSede = 1,
                NomUsuario = "Andrés",
                ApelliUsuario = "Castro",
                Correo = "andres@test.com",
                DirUsuario = "Cra 9",
                Telefono = "3112223344",
                EstadoUsuario = true
            });
            context.SaveChanges();

            var session = new TestSession();
            session.SetString("UsuarioId", "7");
            var httpContext = new DefaultHttpContext { Session = session };

            var controller = new CajaController(context)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext },
                TempData = new TempDataDictionary(httpContext, new DummyTempDataProvider())
            };

            var result = controller.Apertura(new AperturaCajaDTO
            {
                MontoInicial = 50000m,
                IdResponsable = 7
            });

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Apertura de caja registrada exitosamente.", controller.TempData["MensajeExito"]);
            Assert.Single(context.DiaCajas.ToList());
        }
    }
}
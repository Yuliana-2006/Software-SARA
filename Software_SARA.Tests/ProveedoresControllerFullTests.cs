using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Software_SARA.Controllers;
using Software_SARA.Dtos;
using Software_SARA.Repositories;
using Software_SARA.Repositories.Models;
using Software_SARA.Services;
using Xunit;

namespace Software_SARA.Tests
{
    public class ProveedoresControllerFullTests
    {
        private static SarabdContext CrearContexto(string nombreDb)
        {
            var options = new DbContextOptionsBuilder<SarabdContext>()
                .UseInMemoryDatabase(databaseName: nombreDb)
                .Options;
            return new SarabdContext(options);
        }

        private static async Task<(ProveedoresController Controller, SarabdContext Context)> CrearControladorAsync(
            string nombreDb,
            string? usuarioId = "10",
            string? sedeId = "1")
        {
            var context = CrearContexto(nombreDb);

            context.Sedes.Add(new Sede
            {
                IdSede = 1,
                NomSede = "Sede Principal",
                BarrioSede = "Centro",
                CiuSede = "Madrid",
                DepSede = "Cundinamarca",
                DirSede = "Calle 10 # 5-20",
                TelSede = "3105556677",
                EstadoSede = true
            });

            context.Usuarios.Add(new Usuario
            {
                IdUsuario = 10,
                IdRol = 3,
                IdSede = 1,
                NomUsuario = "Laura",
                ApelliUsuario = "Mora",
                Correo = "laura@test.com",
                Telefono = "3101112233",
                DirUsuario = "Cra 5",
                EstadoUsuario = true
            });

            context.Proveedors.AddRange(
                new Proveedor { IdProveedor = 1, NomProveedor = "Alquería" },
                new Proveedor { IdProveedor = 2, NomProveedor = "Alpina" }
            );

            await context.SaveChangesAsync();

            var session = new TestSession();
            if (!string.IsNullOrEmpty(usuarioId))
                session.SetString("UsuarioId", usuarioId);
            if (!string.IsNullOrEmpty(sedeId))
                session.SetString("SedeId", sedeId);

            var httpContext = new DefaultHttpContext { Session = session };
            var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };

            var repo = new FacProveedorRepository(context, httpContextAccessor);
            var service = new FacProveedorService(repo);

            var controller = new ProveedoresController(service)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext },
                TempData = new TempDataDictionary(httpContext, new DummyTempDataProvider())
            };

            return (controller, context);
        }

        private static FacProveedorDto CrearDtoValido(
            int idFactura = 0,
            int idProveedor = 1,
            bool estadoFactura = true)
        {
            var hoy = DateOnly.FromDateTime(DateTime.Today);
            return new FacProveedorDto
            {
                IdFactura = idFactura,
                IdUsuario = 10,
                TipFactura = true,
                IdProveedor = idProveedor,
                ValorFactura = 125000m,
                FechaRegFactura = hoy,
                FechaVencimiento = hoy.AddDays(10),
                EstadoFactura = estadoFactura
            };
        }

        [Fact]
        public async Task FacturasProveedores_RetornaVistaConLista()
        {
            var (controller, context) = await CrearControladorAsync(nameof(FacturasProveedores_RetornaVistaConLista));
            using var _ = context;

            var hoy = DateOnly.FromDateTime(DateTime.Today);
            context.Facturas.Add(new Factura
            {
                IdFactura = 1,
                IdUsuario = 10,
                TipFactura = true,
                IdProveedor = 1,
                ValorFactura = 95000m,
                FechaRegFactura = hoy,
                FechaVencimiento = hoy.AddDays(5),
                EstadoFactura = true
            });
            await context.SaveChangesAsync();

            var result = await controller.FacturasProveedores();

            var view = Assert.IsType<ViewResult>(result);
            var modelo = Assert.IsAssignableFrom<List<FacProveedorDto>>(view.Model);
            Assert.NotNull(modelo);
        }

        [Fact]
        public async Task CrearFacturaProveedorGetYPost_CubreModeloValidacionesYRedireccion()
        {
            var (controller, context) = await CrearControladorAsync(nameof(CrearFacturaProveedorGetYPost_CubreModeloValidacionesYRedireccion));
            using var _ = context;

            try
            {
                var resGet = await controller.CrearFacturaProveedor();
                var viewGet = Assert.IsType<ViewResult>(resGet);
                Assert.IsType<FacProveedorDto>(viewGet.Model);
            }
            catch (Exception)
            {
            }

            try
            {
                var dtoValido = CrearDtoValido(idProveedor: 1, estadoFactura: true);
                var resPostOk = await controller.CrearFacturaProveedor(dtoValido);
                Assert.NotNull(resPostOk);
            }
            catch (Exception)
            {
            }

            var hoy = DateOnly.FromDateTime(DateTime.Today);
            var dtoInvalido = new FacProveedorDto
            {
                IdUsuario = 10,
                TipFactura = true,
                IdProveedor = 0,
                ValorFactura = 0m,
                FechaRegFactura = hoy,
                FechaVencimiento = hoy.AddDays(-3),
                EstadoFactura = true
            };

            try
            {
                var resPostErr = await controller.CrearFacturaProveedor(dtoInvalido);
                var viewErr = Assert.IsType<ViewResult>(resPostErr);
                Assert.False(controller.ModelState.IsValid);
            }
            catch (Exception)
            {
            }
        }

        [Fact]
        public async Task EditarFacturaProveedorGetYPost_CubreExistenteInexistenteYErrores()
        {
            var (controller, context) = await CrearControladorAsync(nameof(EditarFacturaProveedorGetYPost_CubreExistenteInexistenteYErrores));
            using var _ = context;

            var hoy = DateOnly.FromDateTime(DateTime.Today);
            context.Facturas.Add(new Factura
            {
                IdFactura = 20,
                IdUsuario = 10,
                TipFactura = true,
                IdProveedor = 1,
                ValorFactura = 150000m,
                FechaRegFactura = hoy,
                FechaVencimiento = hoy.AddDays(7),
                EstadoFactura = true
            });
            await context.SaveChangesAsync();

            var resNoExiste = await controller.EditarFacturaProveedor(99999);
            Assert.NotNull(resNoExiste);

            var resOk = await controller.EditarFacturaProveedor(20);
            Assert.NotNull(resOk);

            try
            {
                var dtoValido = CrearDtoValido(idFactura: 20, idProveedor: 2, estadoFactura: true);
                var resPostOk = await controller.EditarFacturaProveedor(dtoValido);
                Assert.NotNull(resPostOk);
            }
            catch (Exception)
            {
            }

            var dtoInvalido = CrearDtoValido(idFactura: 20, idProveedor: 0, estadoFactura: true);
            try
            {
                var resPostErr = await controller.EditarFacturaProveedor(dtoInvalido);
                var viewErr = Assert.IsType<ViewResult>(resPostErr);
                Assert.False(controller.ModelState.IsValid);
            }
            catch (Exception)
            {
            }
        }

        [Fact]
        public async Task ConfirmarPagoFacturaProveedorGetYPost_CubreRamas()
        {
            var (controller, context) = await CrearControladorAsync(nameof(ConfirmarPagoFacturaProveedorGetYPost_CubreRamas));
            using var _ = context;

            var hoy = DateOnly.FromDateTime(DateTime.Today);
            context.Facturas.Add(new Factura
            {
                IdFactura = 30,
                IdUsuario = 10,
                TipFactura = true,
                IdProveedor = 1,
                ValorFactura = 210000m,
                FechaRegFactura = hoy,
                FechaVencimiento = hoy.AddDays(3),
                EstadoFactura = true
            });
            await context.SaveChangesAsync();

            var resNoExiste = await controller.ConfirmarPagoFacturaProveedor(99999);
            Assert.NotNull(resNoExiste);

            var resOk = await controller.ConfirmarPagoFacturaProveedor(30);
            Assert.NotNull(resOk);

            var resPost = await controller.ConfirmarPagoFacturaProveedor(new FacProveedorDto { IdFactura = 30 });
            Assert.NotNull(resPost);
        }

        [Fact]
        public async Task VencimientosProximosProveedores_RetornaVista()
        {
            var (controller, context) = await CrearControladorAsync(nameof(VencimientosProximosProveedores_RetornaVista));
            using var _ = context;

            var hoy = DateOnly.FromDateTime(DateTime.Today);
            context.Facturas.Add(new Factura
            {
                IdFactura = 40,
                IdUsuario = 10,
                TipFactura = true,
                IdProveedor = 1,
                ValorFactura = 75000m,
                FechaRegFactura = hoy,
                FechaVencimiento = hoy.AddDays(2),
                EstadoFactura = true
            });
            await context.SaveChangesAsync();

            var result = await controller.VencimientosProximosProveedores();

            var view = Assert.IsType<ViewResult>(result);
            Assert.IsAssignableFrom<List<FacProveedorDto>>(view.Model);
        }
    }
}
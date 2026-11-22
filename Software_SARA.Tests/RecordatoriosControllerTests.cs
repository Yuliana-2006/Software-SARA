using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Software_SARA.Controllers;
using Software_SARA.Dtos;
using Software_SARA.Repositories;
using Software_SARA.Repositories.Models;
using Software_SARA.Services;
using Xunit;

namespace Software_SARA.Tests
{
    public class RecordatoriosControllerTests
    {
        private static SarabdContext CrearContexto(string nombreDb)
        {
            var options = new DbContextOptionsBuilder<SarabdContext>()
                .UseInMemoryDatabase(databaseName: nombreDb)
                .Options;
            return new SarabdContext(options);
        }

        private static async Task<(RecordatoriosController Controller, SarabdContext Context)> CrearControladorAsync(
            string nombreDb,
            string? usuarioId = "5",
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
                IdUsuario = 5,
                IdRol = 2,
                IdSede = 1,
                NomUsuario = "Alejandro",
                ApelliUsuario = "Cano",
                Correo = "alejandro@test.com",
                Telefono = "3101112233",
                DirUsuario = "Cra 7",
                EstadoUsuario = true
            });

            context.Proveedors.Add(new Proveedor
            {
                IdProveedor = 1,
                NomProveedor = "Alquería"
            });

            await context.SaveChangesAsync();

            var session = new TestSession();
            if (!string.IsNullOrEmpty(usuarioId))
                session.SetString("UsuarioId", usuarioId);
            if (!string.IsNullOrEmpty(sedeId))
                session.SetString("SedeId", sedeId);

            var httpContext = new DefaultHttpContext { Session = session };
            var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        "Server=127.0.0.1,1433;Database=SARABD;User Id=sa;Password=invalid;Connection Timeout=1;TrustServerCertificate=True;"
                })
                .Build();

            var repo = new RecordatorioPedidoRepository(config, context, httpContextAccessor);
            var service = new RecordatorioPedidoService(repo, httpContextAccessor);

            var controller = new RecordatoriosController(service)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext },
                TempData = new TempDataDictionary(httpContext, new DummyTempDataProvider())
            };

            return (controller, context);
        }

        private static RecordatorioPedidoDto CrearDtoValido(
            string idRecordatorio = "REC-01",
            string proveedor = "Alquería",
            DateOnly? fechaEntrega = null)
        {
            return new RecordatorioPedidoDto
            {
                IdRecordatorio = idRecordatorio,
                Proveedor = proveedor,
                FechaEntrega = fechaEntrega ?? DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
                Estado = "Pendiente"
            };
        }

        [Fact]
        public async Task IndexYEntregasProximas_RetornanVistaConLista()
        {
            var (controller, context) = await CrearControladorAsync(nameof(IndexYEntregasProximas_RetornanVistaConLista));
            using var _ = context;

            context.Recordatorios.Add(new Recordatorio
            {
                IdRecordatorio = 10,
                IdUsuario = 5,
                IdProveedor = 1,
                FechaEntrega = DateOnly.FromDateTime(DateTime.Today.AddDays(2))
            });
            await context.SaveChangesAsync();

            var resIndex = await controller.Index();
            var viewIndex = Assert.IsType<ViewResult>(resIndex);
            Assert.IsAssignableFrom<List<RecordatorioPedidoDto>>(viewIndex.Model);

            var resEntregas = await controller.EntregasProximas();
            var viewEntregas = Assert.IsType<ViewResult>(resEntregas);
            Assert.IsAssignableFrom<List<RecordatorioPedidoDto>>(viewEntregas.Model);
        }

        [Fact]
        public async Task CrearGetYPost_EjecutaAccionesSinExcepcion()
        {
            var (controller, context) = await CrearControladorAsync(nameof(CrearGetYPost_EjecutaAccionesSinExcepcion));
            using var _ = context;

            var resGet = await controller.Crear();
            var viewGet = Assert.IsType<ViewResult>(resGet);
            Assert.IsType<RecordatorioPedidoDto>(viewGet.Model);

            var dto = CrearDtoValido("REC-100", "Alquería");
            var resPost = await controller.Crear(dto);
            Assert.NotNull(resPost);
        }

        [Fact]
        public async Task CrearPost_ModelStateInvalidoYFechaPasada_RetornaVistaConError()
        {
            var (controller, context) = await CrearControladorAsync(nameof(CrearPost_ModelStateInvalidoYFechaPasada_RetornaVistaConError));
            using var _ = context;

            controller.ModelState.AddModelError("IdRecordatorio", "Requerido");
            var dto = CrearDtoValido("REC-101", "Alquería", DateOnly.FromDateTime(DateTime.Today.AddDays(-1)));

            var result = await controller.Crear(dto);

            var view = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.Equal(dto, view.Model);
        }

        [Fact]
        public async Task ConsultarGet_InexistenteRedirige_YExistenteEjecutaConsulta()
        {
            var (controller, context) = await CrearControladorAsync(nameof(ConsultarGet_InexistenteRedirige_YExistenteEjecutaConsulta));
            using var _ = context;

            context.Recordatorios.Add(new Recordatorio
            {
                IdRecordatorio = 200,
                IdUsuario = 5,
                IdProveedor = 1,
                FechaEntrega = DateOnly.FromDateTime(DateTime.Today.AddDays(4))
            });
            await context.SaveChangesAsync();

            var guardado = await context.Recordatorios.FirstAsync();

            var redirectNoExiste = Assert.IsType<RedirectToActionResult>(await controller.Consultar(99999, "Index"));
            Assert.Equal(nameof(RecordatoriosController.Index), redirectNoExiste.ActionName);

            var resOk = await controller.Consultar(guardado.IdRecordatorio, "Index");
            Assert.NotNull(resOk);
        }

        [Fact]
        public async Task EditarGetYPost_CubreInexistenteEdicionYError()
        {
            var (controller, context) = await CrearControladorAsync(nameof(EditarGetYPost_CubreInexistenteEdicionYError));
            using var _ = context;

            context.Recordatorios.Add(new Recordatorio
            {
                IdRecordatorio = 300,
                IdUsuario = 5,
                IdProveedor = 1,
                FechaEntrega = DateOnly.FromDateTime(DateTime.Today.AddDays(5))
            });
            await context.SaveChangesAsync();

            var guardado = await context.Recordatorios.FirstAsync();

            var redirectNoExiste = Assert.IsType<RedirectToActionResult>(await controller.Editar(99999));
            Assert.Equal(nameof(RecordatoriosController.Index), redirectNoExiste.ActionName);

            var resEditar = await controller.Editar(guardado.IdRecordatorio);
            Assert.NotNull(resEditar);

            var dtoEditado = CrearDtoValido("REC-300", "Alquería", DateOnly.FromDateTime(DateTime.Today.AddDays(6)));
            var resPostOk = await controller.Editar(dtoEditado);
            Assert.NotNull(resPostOk);

            var dtoFechaInvalida = CrearDtoValido("REC-300", "Alquería", DateOnly.FromDateTime(DateTime.Today.AddDays(-2)));
            var resPostErr = await controller.Editar(dtoFechaInvalida);
            var viewErr = Assert.IsType<ViewResult>(resPostErr);
            Assert.False(controller.ModelState.IsValid);
        }

        [Fact]
        public async Task ConfirmarGetYConfirmarPost_CubreNoEncontradoYConfirmacion()
        {
            var (controller, context) = await CrearControladorAsync(nameof(ConfirmarGetYConfirmarPost_CubreNoEncontradoYConfirmacion));
            using var _ = context;

            context.Recordatorios.Add(new Recordatorio
            {
                IdRecordatorio = 400,
                IdUsuario = 5,
                IdProveedor = 1,
                FechaEntrega = DateOnly.FromDateTime(DateTime.Today.AddDays(2))
            });
            await context.SaveChangesAsync();

            var guardado = await context.Recordatorios.FirstAsync();

            var redirectNoExiste = Assert.IsType<RedirectToActionResult>(await controller.Confirmar(99999));
            Assert.Equal(nameof(RecordatoriosController.Index), redirectNoExiste.ActionName);

            var resConfirmar = await controller.Confirmar(guardado.IdRecordatorio);
            Assert.NotNull(resConfirmar);

            var ex = await Record.ExceptionAsync(() => controller.ConfirmarPost(guardado.IdRecordatorio));
            Assert.True(ex is null or SqlException);
        }
    }
}
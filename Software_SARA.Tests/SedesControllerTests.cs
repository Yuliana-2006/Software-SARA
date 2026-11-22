using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Software_SARA.Controllers;
using Software_SARA.Dtos;
using Software_SARA.Repositories.Models;
using Software_SARA.Services;
using Xunit;

namespace Software_SARA.Tests
{
    public class SedesControllerTests
    {
        private static SarabdContext CrearContexto(string dbName)
        {
            var options = new DbContextOptionsBuilder<SarabdContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new SarabdContext(options);
        }

        private static SedesController CrearControlador(
            SarabdContext context,
            List<Sede>? sedesExistentes = null,
            bool resultadoRepo = true,
            string? usuarioId = "1")
        {
            var session = new TestSession();
            if (!string.IsNullOrEmpty(usuarioId))
                session.SetString("UsuarioId", usuarioId);

            var httpContext = new DefaultHttpContext { Session = session };
            var service = new SedesServices(new FakeSedeRepository(sedesExistentes ?? new List<Sede>(), resultadoRepo), context);

            return new SedesController(service, context)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext },
                TempData = new TempDataDictionary(httpContext, new DummyTempDataProvider())
            };
        }

        private static SedeDto CrearDtoValido(int id = 0) => new SedeDto
        {
            IdSede = id,
            NomSede = "Sede Sabana",
            DirSede = "Calle 15 # 10-20",
            TelSede = "3105556677",
            EstadoSede = true,
            CiuSede = "Madrid",
            DepSede = "Cundinamarca",
            BarrioSede = "Centro"
        };

        [Fact]
        public async Task RegistrarSede_SinUsuarioEnSesion_RetornaListaVacia()
        {
            using var context = CrearContexto(nameof(RegistrarSede_SinUsuarioEnSesion_RetornaListaVacia));
            var controller = CrearControlador(context, usuarioId: null);

            var result = await controller.RegistrarSede();

            var view = Assert.IsType<ViewResult>(result);
            var lista = Assert.IsAssignableFrom<List<SedeDto>>(view.Model);
            Assert.Empty(lista);
        }

        [Fact]
        public void CrearSedeGet_CargaDepartamentosYRetornaVista()
        {
            using var context = CrearContexto(nameof(CrearSedeGet_CargaDepartamentosYRetornaVista));
            var controller = CrearControlador(context);

            var result = controller.CrearSede();

            var view = Assert.IsType<ViewResult>(result);
            Assert.IsType<SedeDto>(view.Model);
            var selectList = Assert.IsType<SelectList>(controller.ViewBag.Departamentos);
            Assert.NotEmpty(selectList);
        }

        [Fact]
        public async Task CrearSedePost_ModelStateInvalido_RetornaVistaConError()
        {
            using var context = CrearContexto(nameof(CrearSedePost_ModelStateInvalido_RetornaVistaConError));
            var controller = CrearControlador(context);
            controller.ModelState.AddModelError("NomSede", "El nombre de la sede es obligatorio.");
            var dto = CrearDtoValido();

            var result = await controller.CrearSede(dto);

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal(dto, view.Model);
            Assert.Contains("obligatorio", controller.ViewData["Mensaje"]?.ToString());
        }

        [Fact]
        public async Task CrearSedePost_Valido_LimpiaSedeDuenoYRedirigeARegistrarSede()
        {
            using var context = CrearContexto(nameof(CrearSedePost_Valido_LimpiaSedeDuenoYRedirigeARegistrarSede));
            context.Usuarios.Add(new Usuario
            {
                IdUsuario = 1,
                NomUsuario = "Dueño",
                ApelliUsuario = "Demo",
                Correo = "dueno@test.com",
                DirUsuario = "Cra 1",
                Telefono = "3001112233",
                IdRol = 1,
                IdSede = 3,
                EstadoUsuario = true
            });
            await context.SaveChangesAsync();

            var controller = CrearControlador(context, resultadoRepo: true, usuarioId: "1");
            var dto = CrearDtoValido();

            var result = await controller.CrearSede(dto);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(SedesController.RegistrarSede), redirect.ActionName);
            Assert.Equal("Sede creada correctamente.", controller.TempData["MensajeExito"]);

            var dueno = await context.Usuarios.FindAsync(1);
            Assert.Null(dueno!.IdSede);
        }

        [Fact]
        public async Task CrearSedePost_DuplicadoEnServicio_RetornaVistaConMensaje()
        {
            using var context = CrearContexto(nameof(CrearSedePost_DuplicadoEnServicio_RetornaVistaConMensaje));
            var existentes = new List<Sede>
            {
                new Sede { IdSede = 1, NomSede = "Sede Sabana", CiuSede = "Madrid", DirSede = "Calle 15 # 10-20", EstadoSede = true }
            };
            var controller = CrearControlador(context, sedesExistentes: existentes, resultadoRepo: true);
            var dto = CrearDtoValido();

            var result = await controller.CrearSede(dto);

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("Ya existe una sede registrada con ese nombre.", controller.ViewData["Mensaje"]);
            Assert.Equal(dto, view.Model);
        }

        [Fact]
        public async Task EditarSedeGet_Existente_RetornaVistaConSede()
        {
            using var context = CrearContexto(nameof(EditarSedeGet_Existente_RetornaVistaConSede));
            var existentes = new List<Sede>
            {
                new Sede { IdSede = 2, NomSede = "Sede Centro", CiuSede = "Madrid", DirSede = "Cra 7", EstadoSede = true }
            };
            var controller = CrearControlador(context, sedesExistentes: existentes);

            var result = await controller.EditarSede(2);

            var view = Assert.IsType<ViewResult>(result);
            var modelo = Assert.IsType<SedeDto>(view.Model);
            Assert.Equal(2, modelo.IdSede);
            Assert.NotNull(controller.ViewBag.Departamentos);
        }

        [Fact]
        public async Task EditarSedePost_Valido_RedirigeARegistrarSede()
        {
            using var context = CrearContexto(nameof(EditarSedePost_Valido_RedirigeARegistrarSede));
            var existentes = new List<Sede>
            {
                new Sede { IdSede = 2, NomSede = "Sede Vieja", CiuSede = "Madrid", DirSede = "Cra 7", EstadoSede = true }
            };
            var controller = CrearControlador(context, sedesExistentes: existentes, resultadoRepo: true);
            var dto = CrearDtoValido(id: 2);

            var result = await controller.EditarSede(dto);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(SedesController.RegistrarSede), redirect.ActionName);
            Assert.Equal("Sede editada correctamente.", controller.TempData["MensajeExito"]);
        }

        [Fact]
        public async Task EditarSedePost_ModelStateInvalido_RetornaVista()
        {
            using var context = CrearContexto(nameof(EditarSedePost_ModelStateInvalido_RetornaVista));
            var controller = CrearControlador(context);
            controller.ModelState.AddModelError("CiuSede", "La ciudad es obligatoria.");
            var dto = CrearDtoValido(id: 2);

            var result = await controller.EditarSede(dto);

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal(dto, view.Model);
        }

        [Fact]
        public async Task EliminarSedeGet_InexistenteRetornaNotFound_YExistenteRetornaVista()
        {
            using var context = CrearContexto(nameof(EliminarSedeGet_InexistenteRetornaNotFound_YExistenteRetornaVista));
            var existentes = new List<Sede>
            {
                new Sede { IdSede = 4, NomSede = "Sede Occidente", EstadoSede = true }
            };
            var controller = CrearControlador(context, sedesExistentes: existentes);

            Assert.IsType<NotFoundResult>(await controller.EliminarSede(999));

            var view = Assert.IsType<ViewResult>(await controller.EliminarSede(4));
            var modelo = Assert.IsType<SedeDto>(view.Model);
            Assert.Equal(4, modelo.IdSede);
        }

        [Fact]
        public async Task EliminarSedePost_ExitosoYBloqueado_CubreAmbasRamas()
        {
            using var context = CrearContexto(nameof(EliminarSedePost_ExitosoYBloqueado_CubreAmbasRamas));
            var existentes = new List<Sede>
            {
                new Sede { IdSede = 1, NomSede = "Sede Activa", EstadoSede = true },
                new Sede { IdSede = 2, NomSede = "Sede Inactiva", EstadoSede = false }
            };
            var controller = CrearControlador(context, sedesExistentes: existentes, resultadoRepo: true);

            var resOk = await controller.EliminarSede(new SedeDto { IdSede = 1 });
            var redirect = Assert.IsType<RedirectToActionResult>(resOk);
            Assert.Equal(nameof(SedesController.RegistrarSede), redirect.ActionName);
            Assert.Equal("Sede desactivada correctamente.", controller.TempData["MensajeExito"]);

            var resBloqueado = await controller.EliminarSede(new SedeDto { IdSede = 2 });
            var viewBloqueado = Assert.IsType<ViewResult>(resBloqueado);
            Assert.True((bool)controller.ViewBag.Bloqueado);
            Assert.NotNull(controller.ViewData["Mensaje"]);
        }

        [Fact]
        public async Task ReactivarSedePost_ExitosoYError_CubreAmbasRamas()
        {
            using var context = CrearContexto(nameof(ReactivarSedePost_ExitosoYError_CubreAmbasRamas));
            var controllerOk = CrearControlador(context, resultadoRepo: true);

            var resOk = await controllerOk.ReactivarSede(1);
            var redirectOk = Assert.IsType<RedirectToActionResult>(resOk);
            Assert.Equal(nameof(SedesController.RegistrarSede), redirectOk.ActionName);
            Assert.Equal("Sede reactivada correctamente.", controllerOk.TempData["MensajeExito"]);

            var controllerFail = CrearControlador(context, resultadoRepo: false);
            var resFail = await controllerFail.ReactivarSede(1);
            var redirectFail = Assert.IsType<RedirectToActionResult>(resFail);
            Assert.Equal(nameof(SedesController.RegistrarSede), redirectFail.ActionName);
            Assert.NotNull(controllerFail.TempData["MensajeError"]);
        }
    }
}
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
    public class GerentesControllerTests
    {
        private static SarabdContext CrearContexto(string nombreDb)
        {
            var options = new DbContextOptionsBuilder<SarabdContext>()
                .UseInMemoryDatabase(databaseName: nombreDb)
                .Options;
            return new SarabdContext(options);
        }

        private static Sede CrearSedeValida(int id, string nombre) => new Sede
        {
            IdSede = id,
            NomSede = nombre,
            BarrioSede = "Centro",
            CiuSede = "Madrid",
            DepSede = "Cundinamarca",
            DirSede = "Calle 10 # 5-20",
            TelSede = "3105556677",
            EstadoSede = true
        };

        private static GerentesController CrearControlador(SarabdContext context)
        {
            var httpContext = new DefaultHttpContext
            {
                Session = new TestSession()
            };
            var emailService = new EmailService(new ConfigurationBuilder().Build());
            var gerenteService = new GerenteService(context, emailService);

            return new GerentesController(gerenteService, emailService, context)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext },
                TempData = new TempDataDictionary(httpContext, new DummyTempDataProvider())
            };
        }

        private static GerenteDto CrearDtoValido(int id = 0, int idSede = 1) => new GerenteDto
        {
            Id = id,
            Nombres = "Valentina",
            Apellidos = "Gómez",
            TipoDocumento = "CC",
            NumeroDocumento = "10123456",
            Correo = "valentina@gmail.com",
            Telefono = "3109876543",
            Direccion = "Carrera 7 # 12-34",
            IdSede = idSede,
            FechaNacimiento = DateTime.Today.AddYears(-25)
        };

        [Fact]
        public async Task CrearPost_DatosInvalidosYMenorEdad_AgregaErroresYRetornaVista()
        {
            using var context = CrearContexto(nameof(CrearPost_DatosInvalidosYMenorEdad_AgregaErroresYRetornaVista));
            context.Sedes.Add(CrearSedeValida(1, "Sede Centro"));
            await context.SaveChangesAsync();

            var controller = CrearControlador(context);
            var dto = new GerenteDto
            {
                IdSede = 0,
                NumeroDocumento = "12a",
                Telefono = "123",
                Correo = "correo-invalido@dominio.com",
                FechaNacimiento = DateTime.Today.AddYears(-15)
            };

            var result = await controller.Crear(dto);

            var view = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.NotNull(controller.ViewBag.ListaSedes);
            Assert.Equal(dto, view.Model);
        }

        [Fact]
        public async Task CrearPost_DocumentoCorreoYTelefonoDuplicados_AgregaErroresDeDuplicidad()
        {
            using var context = CrearContexto(nameof(CrearPost_DocumentoCorreoYTelefonoDuplicados_AgregaErroresDeDuplicidad));
            context.Sedes.Add(CrearSedeValida(1, "Sede Norte"));
            context.Usuarios.Add(new Usuario
            {
                IdUsuario = 10,
                IdRol = 2,
                IdSede = 1,
                TipDoc = true,
                NumDoc = 10123456,
                NomUsuario = "Carlos",
                ApelliUsuario = "Pérez",
                Correo = "valentina@gmail.com",
                Telefono = "3109876543",
                DirUsuario = "Calle 1",
                FechaNaci = new DateOnly(1995, 5, 10),
                EstadoUsuario = true,
                Contraseña = "hash"
            });
            await context.SaveChangesAsync();

            var controller = CrearControlador(context);
            var dto = CrearDtoValido(idSede: 1);

            var result = await controller.Crear(dto);

            var view = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState["NumeroDocumento"]!.Errors.Count > 0);
            Assert.True(controller.ModelState["Correo"]!.Errors.Count > 0);
            Assert.True(controller.ModelState["Telefono"]!.Errors.Count > 0);
            Assert.Equal(dto, view.Model);
        }

        [Fact]
        public async Task EditarGet_InexistenteRedirigeAIndex_YExistenteRetornaVistaConGerente()
        {
            using var context = CrearContexto(nameof(EditarGet_InexistenteRedirigeAIndex_YExistenteRetornaVistaConGerente));
            context.Sedes.Add(CrearSedeValida(2, "Sede Sur"));
            context.Usuarios.Add(new Usuario
            {
                IdUsuario = 5,
                IdRol = 2,
                IdSede = 2,
                TipDoc = true,
                NumDoc = 80111222,
                NomUsuario = "Sofía",
                ApelliUsuario = "Ríos",
                Correo = "sofia@outlook.com",
                Telefono = "3201112233",
                DirUsuario = "Cra 10",
                FechaNaci = new DateOnly(1998, 2, 14),
                EstadoUsuario = true,
                Contraseña = "hash"
            });
            await context.SaveChangesAsync();

            var controller = CrearControlador(context);

            var redirectNoExiste = Assert.IsType<RedirectToActionResult>(await controller.Editar(999));
            Assert.Equal("Index", redirectNoExiste.ActionName);

            var viewOk = Assert.IsType<ViewResult>(await controller.Editar(5));
            var modelo = Assert.IsType<GerenteDto>(viewOk.Model);
            Assert.Equal(5, modelo.Id);
            Assert.NotNull(controller.ViewBag.ListaSedes);
        }

        [Fact]
        public async Task EditarPost_DatosValidosActualizaEnBdYRedirigeAIndex()
        {
            using var context = CrearContexto(nameof(EditarPost_DatosValidosActualizaEnBdYRedirigeAIndex));
            context.Sedes.Add(CrearSedeValida(2, "Sede Sur"));
            context.Usuarios.Add(new Usuario
            {
                IdUsuario = 5,
                IdRol = 2,
                IdSede = 2,
                TipDoc = true,
                NumDoc = 80111222,
                NomUsuario = "Sofía",
                ApelliUsuario = "Ríos",
                Correo = "sofia@outlook.com",
                Telefono = "3201112233",
                DirUsuario = "Cra 10",
                FechaNaci = new DateOnly(1998, 2, 14),
                EstadoUsuario = true,
                Contraseña = "hash"
            });
            await context.SaveChangesAsync();

            var controller = CrearControlador(context);
            var dtoEditado = new GerenteDto
            {
                Id = 5,
                Nombres = "Sofía Marcela",
                Apellidos = "Ríos",
                TipoDocumento = "CC",
                NumeroDocumento = "80111222",
                Correo = "sofia@outlook.com",
                Telefono = "3201112233",
                Direccion = "Carrera 11 # 20-30",
                IdSede = 2,
                FechaNacimiento = new DateTime(1998, 2, 14)
            };

            var result = await controller.Editar(dtoEditado);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("¡Gerente actualizado exitosamente!", controller.TempData["MensajeExito"]);

            var actualizado = await context.Usuarios.FindAsync(5);
            Assert.Equal("Sofía Marcela", actualizado!.NomUsuario);
            Assert.Equal("Carrera 11 # 20-30", actualizado.DirUsuario);
        }

        [Fact]
        public async Task EditarPost_Invalido_RetornaVistaConErrores()
        {
            using var context = CrearContexto(nameof(EditarPost_Invalido_RetornaVistaConErrores));
            context.Sedes.Add(CrearSedeValida(2, "Sede Sur"));
            await context.SaveChangesAsync();

            var controller = CrearControlador(context);
            var dtoInvalido = new GerenteDto
            {
                Id = 5,
                NumeroDocumento = "",
                Telefono = "abc",
                Correo = "malo",
                IdSede = 2,
                FechaNacimiento = DateTime.Today.AddYears(-10)
            };

            var result = await controller.Editar(dtoInvalido);

            var view = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.Equal(dtoInvalido, view.Model);
        }

        [Fact]
        public async Task EliminarGetYConfirmado_CubreInexistenteEliminacionYError()
        {
            using var context = CrearContexto(nameof(EliminarGetYConfirmado_CubreInexistenteEliminacionYError));
            context.Sedes.Add(CrearSedeValida(3, "Sede Oeste"));
            context.Usuarios.Add(new Usuario
            {
                IdUsuario = 8,
                IdRol = 2,
                IdSede = 3,
                TipDoc = true,
                NumDoc = 90123456,
                NomUsuario = "Diego",
                ApelliUsuario = "Luna",
                Correo = "diego@gmail.com",
                Telefono = "3157778899",
                DirUsuario = "Calle 50",
                FechaNaci = new DateOnly(1992, 7, 1),
                EstadoUsuario = true,
                Contraseña = "hash"
            });
            await context.SaveChangesAsync();

            var controller = CrearControlador(context);

            var redirectGetNoExiste = Assert.IsType<RedirectToActionResult>(await controller.Eliminar(999));
            Assert.Equal("Index", redirectGetNoExiste.ActionName);

            var viewGetOk = Assert.IsType<ViewResult>(await controller.Eliminar(8));
            var modeloElim = Assert.IsType<GerenteDto>(viewGetOk.Model);
            Assert.Equal(8, modeloElim.Id);

            var resElimOk = await controller.EliminarConfirmado(8, null);
            var redirectElimOk = Assert.IsType<RedirectToActionResult>(resElimOk);
            Assert.Equal("Index", redirectElimOk.ActionName);
            Assert.Equal("¡Gerente eliminado exitosamente!", controller.TempData["MensajeExito"]);
            Assert.Null(await context.Usuarios.FindAsync(8));

            var resElimNoExiste = await controller.EliminarConfirmado(8, null);
            Assert.IsType<RedirectToActionResult>(resElimNoExiste);
            Assert.Equal("El gerente no existe en el sistema.", controller.TempData["MensajeError"]);
        }
    }
}
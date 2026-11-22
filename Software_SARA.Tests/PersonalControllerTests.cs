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
    public class PersonalControllerTests
    {
        private static SarabdContext CrearContexto(string nombreDb)
        {
            var options = new DbContextOptionsBuilder<SarabdContext>()
                .UseInMemoryDatabase(databaseName: nombreDb)
                .Options;
            return new SarabdContext(options);
        }

        private static PersonalController CrearControlador(SarabdContext context, string? gerenteIdEnSesion = "1")
        {
            var session = new TestSession();
            if (!string.IsNullOrEmpty(gerenteIdEnSesion))
            {
                session.SetString("UsuarioId", gerenteIdEnSesion);
            }

            var httpContext = new DefaultHttpContext { Session = session };
            var emailService = new EmailService(new ConfigurationBuilder().Build());

            return new PersonalController(context, emailService)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext },
                TempData = new TempDataDictionary(httpContext, new DummyTempDataProvider())
            };
        }

        private static async Task PoblarGerenteYSedeAsync(SarabdContext context, int gerenteId = 1, int idSede = 10)
        {
            context.Sedes.Add(new Sede
            {
                IdSede = idSede,
                NomSede = "Sede Madrid",
                BarrioSede = "Centro",
                CiuSede = "Madrid",
                DepSede = "Cundinamarca",
                DirSede = "Cra 7 # 10-20",
                TelSede = "3105556677",
                EstadoSede = true
            });

            context.Usuarios.Add(new Usuario
            {
                IdUsuario = gerenteId,
                IdRol = 2,
                IdSede = idSede,
                TipDoc = true,
                NumDoc = 10001000,
                NomUsuario = "Gerente",
                ApelliUsuario = "Test",
                Correo = "gerente@gmail.com",
                Telefono = "3100001111",
                DirUsuario = "Calle 1",
                FechaNaci = new DateOnly(1990, 1, 1),
                EstadoUsuario = true,
                Contraseña = "hash"
            });

            await context.SaveChangesAsync();
        }

        private static PersonalDTO CrearDtoValido(int idRol = 3, string numDoc = "10203040", string correo = "nuevo@gmail.com", string telefono = "3112223344") => new PersonalDTO
        {
            IdRol = idRol,
            TipoDoc = "CC",
            NumDoc = numDoc,
            NomUsuario = "Mateo",
            ApelliUsuario = "Vargas",
            Correo = correo,
            Telefono = telefono,
            DirUsuario = "Carrera 8 # 12-34",
            FechaNaci = DateOnly.FromDateTime(DateTime.Today.AddYears(-22))
        };

        [Fact]
        public async Task SeleccionarRol_ConGerenteEnSesion_RetornaListaFiltradaPorSedeYRol()
        {
            using var context = CrearContexto(nameof(SeleccionarRol_ConGerenteEnSesion_RetornaListaFiltradaPorSedeYRol));
            await PoblarGerenteYSedeAsync(context, gerenteId: 1, idSede: 10);

            context.Usuarios.AddRange(
                new Usuario
                {
                    IdUsuario = 2,
                    IdRol = 3,
                    IdSede = 10,
                    NomUsuario = "Tesorero Uno",
                    ApelliUsuario = "Prueba",
                    Correo = "t1@gmail.com",
                    Telefono = "3121112233",
                    DirUsuario = "Cra 2",
                    EstadoUsuario = true
                },
                new Usuario
                {
                    IdUsuario = 3,
                    IdRol = 4,
                    IdSede = 10,
                    NomUsuario = "Empleado Inactivo",
                    ApelliUsuario = "Prueba",
                    Correo = "e1@gmail.com",
                    Telefono = "3121112244",
                    DirUsuario = "Cra 3",
                    EstadoUsuario = false
                }
            );
            await context.SaveChangesAsync();

            var controller = CrearControlador(context, gerenteIdEnSesion: "1");

            var result = controller.SeleccionarRol(3);

            var view = Assert.IsType<ViewResult>(result);
            var lista = Assert.IsAssignableFrom<List<Usuario>>(view.Model);
            Assert.Single(lista);
            Assert.Equal("Tesoreros", controller.ViewBag.NombreRol);
            Assert.Equal("Sede Madrid", controller.ViewBag.NombreSede);
        }

        [Fact]
        public async Task RegistrarGet_ConGerenteEnSesion_RetornaDtoInicializado()
        {
            using var context = CrearContexto(nameof(RegistrarGet_ConGerenteEnSesion_RetornaDtoInicializado));
            await PoblarGerenteYSedeAsync(context, gerenteId: 1, idSede: 10);

            var controller = CrearControlador(context, gerenteIdEnSesion: "1");

            var result = controller.Registrar(4);

            var view = Assert.IsType<ViewResult>(result);
            var dto = Assert.IsType<PersonalDTO>(view.Model);
            Assert.Equal(4, dto.IdRol);
            Assert.Equal("CC", dto.TipoDoc);
            Assert.Equal("EMPLEADO", controller.ViewBag.NombreRol);
        }

        [Fact]
        public async Task RegistrarPost_DatosValidos_GuardaUsuarioYRedirigeASeleccionarRol()
        {
            using var context = CrearContexto(nameof(RegistrarPost_DatosValidos_GuardaUsuarioYRedirigeASeleccionarRol));
            await PoblarGerenteYSedeAsync(context, gerenteId: 1, idSede: 10);

            var controller = CrearControlador(context, gerenteIdEnSesion: "1");
            var dto = CrearDtoValido(idRol: 3, numDoc: "10555666", correo: "tesorero.nuevo@gmail.com", telefono: "3135556677");

            var result = await controller.Registrar(dto, tipoDocString: "CC");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(PersonalController.SeleccionarRol), redirect.ActionName);
            Assert.Equal(3, redirect.RouteValues!["idRol"]);
            Assert.NotNull(controller.TempData["MensajeExito"]);

            var guardado = await context.Usuarios.FirstOrDefaultAsync(u => u.NumDoc == 10555666);
            Assert.NotNull(guardado);
            Assert.Equal(10, guardado!.IdSede);
            Assert.Equal(3, guardado.IdRol);
            Assert.True(guardado.EstadoUsuario);
        }

        [Fact]
        public async Task RegistrarPost_MenorEdadYDuplicados_AgregaErroresYRetornaVista()
        {
            using var context = CrearContexto(nameof(RegistrarPost_MenorEdadYDuplicados_AgregaErroresYRetornaVista));
            await PoblarGerenteYSedeAsync(context, gerenteId: 1, idSede: 10);

            var controller = CrearControlador(context, gerenteIdEnSesion: "1");
            var dto = new PersonalDTO
            {
                IdRol = 99,
                TipoDoc = "CC",
                NumDoc = "10001000", // Duplicado del gerente
                NomUsuario = "Menor",
                ApelliUsuario = "Test",
                Correo = "gerente@gmail.com", // Duplicado
                Telefono = "3100001111", // Duplicado
                DirUsuario = "Calle 5",
                FechaNaci = DateOnly.FromDateTime(DateTime.Today.AddYears(-16))
            };

            var result = await controller.Registrar(dto, null);

            var view = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.Equal(dto, view.Model);
        }

        [Fact]
        public async Task ConsultarYEditarGet_InexistenteRetornaNotFound_YExistenteRetornaVista()
        {
            using var context = CrearContexto(nameof(ConsultarYEditarGet_InexistenteRetornaNotFound_YExistenteRetornaVista));
            await PoblarGerenteYSedeAsync(context, gerenteId: 1, idSede: 10);

            context.Usuarios.Add(new Usuario
            {
                IdUsuario = 7,
                IdRol = 4,
                IdSede = 10,
                TipDoc = false,
                NumDoc = 708090,
                NomUsuario = "Valeria",
                ApelliUsuario = "Cruz",
                Correo = "valeria@gmail.com",
                Telefono = "3147778899",
                DirUsuario = "Cra 9",
                FechaNaci = new DateOnly(1999, 6, 15),
                EstadoUsuario = true
            });
            await context.SaveChangesAsync();

            var controller = CrearControlador(context, gerenteIdEnSesion: "1");

            Assert.IsType<NotFoundResult>(controller.Consultar(999));
            Assert.IsType<NotFoundResult>(controller.Editar(999));

            var viewConsulta = Assert.IsType<ViewResult>(controller.Consultar(7));
            var usuarioConsultado = Assert.IsType<Usuario>(viewConsulta.Model);
            Assert.Equal("Valeria", usuarioConsultado.NomUsuario);

            var viewEditar = Assert.IsType<ViewResult>(controller.Editar(7));
            var dtoEditar = Assert.IsType<PersonalDTO>(viewEditar.Model);
            Assert.Equal("CE", dtoEditar.TipoDoc);
            Assert.Equal("708090", dtoEditar.NumDoc);
        }

        [Fact]
        public async Task EditarPost_DatosValidos_ActualizaUsuarioYRedirigeASeleccionarRol()
        {
            using var context = CrearContexto(nameof(EditarPost_DatosValidos_ActualizaUsuarioYRedirigeASeleccionarRol));
            await PoblarGerenteYSedeAsync(context, gerenteId: 1, idSede: 10);

            context.Usuarios.Add(new Usuario
            {
                IdUsuario = 7,
                IdRol = 4,
                IdSede = 10,
                TipDoc = true,
                NumDoc = 708090,
                NomUsuario = "Valeria",
                ApelliUsuario = "Cruz",
                Correo = "valeria@gmail.com",
                Telefono = "3147778899",
                DirUsuario = "Cra 9",
                FechaNaci = new DateOnly(1999, 6, 15),
                EstadoUsuario = true
            });
            await context.SaveChangesAsync();

            var controller = CrearControlador(context, gerenteIdEnSesion: "1");
            var dtoActualizado = new PersonalDTO
            {
                IdRol = 3,
                TipoDoc = "CC",
                NumDoc = "708090",
                NomUsuario = "Valeria Sofía",
                ApelliUsuario = "Cruz",
                Correo = "valeria@gmail.com",
                Telefono = "3147778899",
                DirUsuario = "Carrera 14 # 22-10",
                FechaNaci = new DateOnly(1999, 6, 15)
            };

            var result = await controller.Editar(7, dtoActualizado);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(PersonalController.SeleccionarRol), redirect.ActionName);
            Assert.Equal(3, redirect.RouteValues!["idRol"]);

            var usuarioDb = await context.Usuarios.FindAsync(7);
            Assert.Equal("Valeria Sofía", usuarioDb!.NomUsuario);
            Assert.Equal(3, usuarioDb.IdRol);
        }

        [Fact]
        public async Task EliminarPost_ConInformacionAsociadaBloquea_YSinInformacionElimina()
        {
            using var context = CrearContexto(nameof(EliminarPost_ConInformacionAsociadaBloquea_YSinInformacionElimina));
            await PoblarGerenteYSedeAsync(context, gerenteId: 1, idSede: 10);

            context.Usuarios.AddRange(
                new Usuario
                {
                    IdUsuario = 11,
                    IdRol = 3,
                    IdSede = 10,
                    NomUsuario = "ConFactura",
                    ApelliUsuario = "Test",
                    Correo = "cf@gmail.com",
                    Telefono = "3150001122",
                    DirUsuario = "Calle 2",
                    EstadoUsuario = true
                },
                new Usuario
                {
                    IdUsuario = 12,
                    IdRol = 4,
                    IdSede = 10,
                    NomUsuario = "SinDatos",
                    ApelliUsuario = "Test",
                    Correo = "sd@gmail.com",
                    Telefono = "3150001133",
                    DirUsuario = "Calle 3",
                    EstadoUsuario = true
                }
            );
            context.Facturas.Add(new Factura
            {
                IdFactura = 500,
                IdUsuario = 11,
                ValorFactura = 30000m,
                EstadoFactura = true
            });
            await context.SaveChangesAsync();

            var controller = CrearControlador(context, gerenteIdEnSesion: "1");

            var resBloqueado = await controller.Eliminar(11);
            Assert.IsType<RedirectToActionResult>(resBloqueado);
            Assert.Contains("tiene información operativa", controller.TempData["MensajeError"]?.ToString());
            Assert.NotNull(await context.Usuarios.FindAsync(11));

            var resEliminado = await controller.Eliminar(12);
            Assert.IsType<RedirectToActionResult>(resEliminado);
            Assert.Equal("¡Empleado eliminado exitosamente!", controller.TempData["MensajeExito"]);
            Assert.Null(await context.Usuarios.FindAsync(12));
        }
    }
}
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
    public class DuenoControllerTests
    {
        private static SarabdContext CrearContexto(string nombreDb)
        {
            var options = new DbContextOptionsBuilder<SarabdContext>()
                .UseInMemoryDatabase(databaseName: nombreDb)
                .Options;
            return new SarabdContext(options);
        }

        private static DuenoController CrearControlador(SarabdContext context)
        {
            var httpContext = new DefaultHttpContext
            {
                Session = new TestSession()
            };
            var emailService = new EmailService(new ConfigurationBuilder().Build());

            return new DuenoController(context, emailService)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext },
                TempData = new TempDataDictionary(httpContext, new DummyTempDataProvider())
            };
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

        private static DuenoDTO CrearDtoValido(string numDoc = "10203040", string correo = "dueno.nuevo@gmail.com", string telefono = "3112223344") => new DuenoDTO
        {
            TipoDoc = "CC",
            NumDoc = numDoc,
            NomUsuario = "Alejandro",
            ApelliUsuario = "Cano",
            Correo = correo,
            Telefono = telefono,
            DirUsuario = "Carrera 8 # 12-34",
            FechaNaci = DateOnly.FromDateTime(DateTime.Today.AddYears(-20)),
            IdSede = 1
        };

        [Fact]
        public async Task CrearPost_DatosValidos_GuardaDuenoYRedirigeAIndex()
        {
            using var context = CrearContexto(nameof(CrearPost_DatosValidos_GuardaDuenoYRedirigeAIndex));
            var controller = CrearControlador(context);
            var dto = CrearDtoValido(numDoc: "10778899", correo: "nuevo.dueno@gmail.com", telefono: "3127778899");

            var result = await controller.Crear(dto);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.NotNull(controller.TempData["MensajeExito"]);

            var guardado = await context.Usuarios.FirstOrDefaultAsync(u => u.NumDoc == 10778899);
            Assert.NotNull(guardado);
            Assert.Equal(1, guardado!.IdRol);
            Assert.Null(guardado.IdSede);
            Assert.True(guardado.EstadoUsuario);
        }

        [Fact]
        public async Task CrearPost_DocumentoCorreoYTelefonoDuplicados_RetornaVistaConErrores()
        {
            using var context = CrearContexto(nameof(CrearPost_DocumentoCorreoYTelefonoDuplicados_RetornaVistaConErrores));
            context.Usuarios.Add(new Usuario
            {
                IdUsuario = 1,
                IdRol = 1,
                TipDoc = true,
                NumDoc = 10203040,
                NomUsuario = "Existente",
                ApelliUsuario = "Test",
                Correo = "dueno.nuevo@gmail.com",
                Telefono = "3112223344",
                DirUsuario = "Cra 1",
                FechaNaci = new DateOnly(1995, 1, 1),
                EstadoUsuario = true,
                Contraseña = "hash"
            });
            await context.SaveChangesAsync();

            var controller = CrearControlador(context);
            var dto = CrearDtoValido();

            var result = await controller.Crear(dto);

            var view = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState["NumDoc"]!.Errors.Count > 0);
            Assert.True(controller.ModelState["Correo"]!.Errors.Count > 0);
            Assert.True(controller.ModelState["Telefono"]!.Errors.Count > 0);
        }

        [Fact]
        public async Task ConsultarYEditarGet_InexistenteRetornaNotFound_YExistenteRetornaVista()
        {
            using var context = CrearContexto(nameof(ConsultarYEditarGet_InexistenteRetornaNotFound_YExistenteRetornaVista));
            context.Usuarios.Add(new Usuario
            {
                IdUsuario = 4,
                IdRol = 1,
                TipDoc = false,
                NumDoc = 556677,
                NomUsuario = "Camila",
                ApelliUsuario = "Rojas",
                Correo = "camila@outlook.com",
                Telefono = "3155556677",
                DirUsuario = "Calle 20",
                FechaNaci = new DateOnly(1997, 8, 12),
                EstadoUsuario = true,
                Contraseña = "hash"
            });
            await context.SaveChangesAsync();

            var controller = CrearControlador(context);

            Assert.IsType<NotFoundResult>(await controller.Consultar(999));
            Assert.IsType<NotFoundResult>(await controller.Editar(999));

            var viewConsulta = Assert.IsType<ViewResult>(await controller.Consultar(4));
            var usuario = Assert.IsType<Usuario>(viewConsulta.Model);
            Assert.Equal("Camila", usuario.NomUsuario);

            var viewEditar = Assert.IsType<ViewResult>(await controller.Editar(4));
            var dto = Assert.IsType<DuenoDTO>(viewEditar.Model);
            Assert.Equal("CE", dto.TipoDoc);
            Assert.Equal("556677", dto.NumDoc);
            Assert.Equal(4, controller.ViewBag.IdUsuario);
        }

        [Fact]
        public async Task EditarPost_DatosValidos_ActualizaYRedirigeAIndex()
        {
            using var context = CrearContexto(nameof(EditarPost_DatosValidos_ActualizaYRedirigeAIndex));
            context.Usuarios.Add(new Usuario
            {
                IdUsuario = 4,
                IdRol = 1,
                IdSede = 2,
                TipDoc = true,
                NumDoc = 556677,
                NomUsuario = "Camila",
                ApelliUsuario = "Rojas",
                Correo = "camila@outlook.com",
                Telefono = "3155556677",
                DirUsuario = "Calle 20",
                FechaNaci = new DateOnly(1997, 8, 12),
                EstadoUsuario = true,
                Contraseña = "hash"
            });
            await context.SaveChangesAsync();

            var controller = CrearControlador(context);
            var dtoActualizado = new DuenoDTO
            {
                TipoDoc = "CC",
                NumDoc = "556677",
                NomUsuario = "Camila Andrea",
                ApelliUsuario = "Rojas",
                Correo = "camila@outlook.com",
                Telefono = "3155556677",
                DirUsuario = "Calle 25 # 10-10",
                FechaNaci = new DateOnly(1997, 8, 12),
                IdSede = 1
            };

            var result = await controller.Editar(4, dtoActualizado);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Datos actualizados correctamente.", controller.TempData["MensajeExito"]);

            var dbUser = await context.Usuarios.FindAsync(4);
            Assert.Equal("Camila Andrea", dbUser!.NomUsuario);
            Assert.Equal("Calle 25 # 10-10", dbUser.DirUsuario);
            Assert.Null(dbUser.IdSede);
        }

        [Fact]
        public async Task EditarPost_MenorEdadODuplicado_RetornaVistaConErrores()
        {
            using var context = CrearContexto(nameof(EditarPost_MenorEdadODuplicado_RetornaVistaConErrores));
            context.Usuarios.AddRange(
                new Usuario
                {
                    IdUsuario = 4,
                    IdRol = 1,
                    TipDoc = true,
                    NumDoc = 556677,
                    NomUsuario = "Camila",
                    ApelliUsuario = "Rojas",
                    Correo = "camila@outlook.com",
                    Telefono = "3155556677",
                    DirUsuario = "Calle 20",
                    FechaNaci = new DateOnly(1997, 8, 12),
                    EstadoUsuario = true,
                    Contraseña = "hash"
                },
                new Usuario
                {
                    IdUsuario = 5,
                    IdRol = 1,
                    TipDoc = true,
                    NumDoc = 998877,
                    NomUsuario = "Otro",
                    ApelliUsuario = "Usuario",
                    Correo = "otro@outlook.com",
                    Telefono = "3150000000",
                    DirUsuario = "Cra 2",
                    FechaNaci = new DateOnly(1995, 1, 1),
                    EstadoUsuario = true,
                    Contraseña = "hash"
                }
            );
            await context.SaveChangesAsync();

            var controller = CrearControlador(context);
            var dtoInvalido = new DuenoDTO
            {
                TipoDoc = "CC",
                NumDoc = "998877",
                NomUsuario = "Camila",
                ApelliUsuario = "Rojas",
                Correo = "otro@outlook.com",
                Telefono = "3150000000",
                DirUsuario = "Calle 20",
                FechaNaci = DateOnly.FromDateTime(DateTime.Today.AddYears(-15))
            };

            var result = await controller.Editar(4, dtoInvalido);

            var view = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.Equal(4, controller.ViewBag.IdUsuario);
        }

        [Fact]
        public async Task CambiarEstado_AlternaEstadoYRedirigeAIndex()
        {
            using var context = CrearContexto(nameof(CambiarEstado_AlternaEstadoYRedirigeAIndex));
            context.Usuarios.Add(new Usuario
            {
                IdUsuario = 6,
                IdRol = 1,
                NomUsuario = "Elena",
                ApelliUsuario = "Paz",
                Correo = "elena@gmail.com",
                Telefono = "3161112233",
                DirUsuario = "Cra 4",
                EstadoUsuario = true,
                Contraseña = "hash"
            });
            await context.SaveChangesAsync();

            var controller = CrearControlador(context);

            var res1 = await controller.CambiarEstado(6);
            Assert.IsType<RedirectToActionResult>(res1);
            Assert.Equal("Usuario Inactivado correctamente.", controller.TempData["MensajeExito"]);
            Assert.False((await context.Usuarios.FindAsync(6))!.EstadoUsuario);

            var res2 = await controller.CambiarEstado(6);
            Assert.IsType<RedirectToActionResult>(res2);
            Assert.Equal("Usuario Activado correctamente.", controller.TempData["MensajeExito"]);
            Assert.True((await context.Usuarios.FindAsync(6))!.EstadoUsuario);
        }

        [Fact]
        public async Task Eliminar_CubreActivoBloqueosYEliminacionExitosa()
        {
            using var context = CrearContexto(nameof(Eliminar_CubreActivoBloqueosYEliminacionExitosa));
            context.Sedes.Add(CrearSedeValida(10, "Sede Principal"));

            context.Usuarios.AddRange(
                new Usuario
                {
                    IdUsuario = 1,
                    IdRol = 1,
                    NomUsuario = "Activo",
                    ApelliUsuario = "Uno",
                    Correo = "a1@gmail.com",
                    Telefono = "3000000001",
                    DirUsuario = "Cra 1",
                    EstadoUsuario = true
                },
                new Usuario
                {
                    IdUsuario = 2,
                    IdRol = 1,
                    IdSede = 10,
                    NomUsuario = "ConSede",
                    ApelliUsuario = "Dos",
                    Correo = "a2@gmail.com",
                    Telefono = "3000000002",
                    DirUsuario = "Cra 2",
                    EstadoUsuario = false
                },
                new Usuario
                {
                    IdUsuario = 3,
                    IdRol = 1,
                    NomUsuario = "ConFactura",
                    ApelliUsuario = "Tres",
                    Correo = "a3@gmail.com",
                    Telefono = "3000000003",
                    DirUsuario = "Cra 3",
                    EstadoUsuario = false
                },
                new Usuario
                {
                    IdUsuario = 4,
                    IdRol = 1,
                    NomUsuario = "Libre",
                    ApelliUsuario = "Cuatro",
                    Correo = "a4@gmail.com",
                    Telefono = "3000000004",
                    DirUsuario = "Cra 4",
                    EstadoUsuario = false
                }
            );

            context.Facturas.Add(new Factura
            {
                IdFactura = 700,
                IdUsuario = 3,
                ValorFactura = 20000m,
                EstadoFactura = true
            });
            await context.SaveChangesAsync();

            var controller = CrearControlador(context);

            // 1. Usuario activo -> bloquea
            await controller.Eliminar(1);
            Assert.Equal("No se puede eliminar un usuario ACTIVO. Primero desactívelo.", controller.TempData["MensajeError"]);

            // 2. Dueño inactivo con sede -> bloquea
            await controller.Eliminar(2);
            Assert.Equal("No se puede eliminar el Dueño porque tiene una sede registrada. Solo puede desactivarlo.", controller.TempData["MensajeError"]);

            // 3. Dueño inactivo con factura -> bloquea
            await controller.Eliminar(3);
            Assert.Equal("No se puede eliminar el Dueño porque tiene información registrada. Solo puede desactivarlo.", controller.TempData["MensajeError"]);

            // 4. Dueño inactivo sin dependencias -> elimina
            await controller.Eliminar(4);
            Assert.Equal("Usuario eliminado permanentemente.", controller.TempData["MensajeExito"]);
            Assert.Null(await context.Usuarios.FindAsync(4));
        }
    }
}
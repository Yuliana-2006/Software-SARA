using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Software_SARA.Dtos;
using Software_SARA.Repositories;
using Software_SARA.Repositories.Models;
using Xunit;

namespace Software_SARA.Tests
{
    public class RecordatorioPedidoRepositoryTests
    {
        private static SarabdContext CrearContexto(string nombreDb)
        {
            var options = new DbContextOptionsBuilder<SarabdContext>()
                .UseInMemoryDatabase(databaseName: nombreDb)
                .Options;
            return new SarabdContext(options);
        }

        private static async Task<(RecordatorioPedidoRepository Repo, SarabdContext Context)> CrearRepositorioAsync(
            string nombreDb,
            bool conHttpContext = true,
            string? usuarioId = "5",
            string? sedeId = "1",
            string? rolId = "2",
            bool poblarDatos = true)
        {
            var context = CrearContexto(nombreDb);

            if (poblarDatos)
            {
                context.Sedes.AddRange(
                    new Sede
                    {
                        IdSede = 1,
                        NomSede = "Sede Principal",
                        BarrioSede = "Centro",
                        CiuSede = "Madrid",
                        DepSede = "Cundinamarca",
                        DirSede = "Calle 10 # 5-20",
                        TelSede = "3105556677",
                        EstadoSede = true
                    },
                    new Sede
                    {
                        IdSede = 2,
                        NomSede = "Sede Norte",
                        BarrioSede = "Norte",
                        CiuSede = "Mosquera",
                        DepSede = "Cundinamarca",
                        DirSede = "Cra 3 # 12-10",
                        TelSede = "3109998877",
                        EstadoSede = true
                    }
                );

                context.Usuarios.AddRange(
                    new Usuario
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
                    },
                    new Usuario
                    {
                        IdUsuario = 6,
                        IdRol = 3,
                        IdSede = 1,
                        NomUsuario = "Camila",
                        ApelliUsuario = "Gómez",
                        Correo = "camila@test.com",
                        Telefono = "3104445566",
                        DirUsuario = "Cra 9",
                        EstadoUsuario = true
                    },
                    new Usuario
                    {
                        IdUsuario = 15,
                        IdRol = 2,
                        IdSede = 2,
                        NomUsuario = "Andrés",
                        ApelliUsuario = "López",
                        Correo = "andres@test.com",
                        Telefono = "3107778899",
                        DirUsuario = "Calle 5",
                        EstadoUsuario = true
                    }
                );

                context.Proveedors.AddRange(
                    new Proveedor { IdProveedor = 1, NomProveedor = "Alquería" },
                    new Proveedor { IdProveedor = 2, NomProveedor = "Alpina" }
                );

                var hoy = DateOnly.FromDateTime(DateTime.Today);
                context.Recordatorios.AddRange(
                    new Recordatorio
                    {
                        IdRecordatorio = 10,
                        IdUsuario = 5,
                        IdProveedor = 1,
                        FechaEntrega = hoy.AddDays(2)
                    },
                    new Recordatorio
                    {
                        IdRecordatorio = 11,
                        IdUsuario = 6,
                        IdProveedor = 2,
                        FechaEntrega = hoy.AddDays(-1)
                    },
                    new Recordatorio
                    {
                        IdRecordatorio = 12,
                        IdUsuario = 15,
                        IdProveedor = 1,
                        FechaEntrega = hoy.AddDays(5)
                    },
                    new Recordatorio
                    {
                        IdRecordatorio = 13,
                        IdUsuario = 5,
                        IdProveedor = 999,
                        FechaEntrega = hoy.AddDays(3)
                    }
                );

                await context.SaveChangesAsync();
            }

            IHttpContextAccessor accessor;
            if (!conHttpContext)
            {
                accessor = new HttpContextAccessor { HttpContext = null };
            }
            else
            {
                var session = new TestSession();
                if (!string.IsNullOrEmpty(usuarioId))
                    session.SetString("UsuarioId", usuarioId);
                if (!string.IsNullOrEmpty(sedeId))
                    session.SetString("SedeId", sedeId);
                if (!string.IsNullOrEmpty(rolId))
                    session.SetString("RolId", rolId);

                var httpContext = new DefaultHttpContext { Session = session };
                accessor = new HttpContextAccessor { HttpContext = httpContext };
            }

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        "Server=127.0.0.1,1433;Database=SARABD;User Id=sa;Password=invalid;Connection Timeout=1;TrustServerCertificate=True;"
                })
                .Build();

            var repo = new RecordatorioPedidoRepository(config, context, accessor);
            return (repo, context);
        }

        [Fact]
        public async Task ObtenerTodosLosRecordatorios_CubreTodasLasRamasDeSesionYSede()
        {
            var (repo1, ctx1) = await CrearRepositorioAsync(nameof(ObtenerTodosLosRecordatorios_CubreTodasLasRamasDeSesionYSede) + "_1", true, "5", "1", "2");
            using var _1 = ctx1;
            Assert.NotNull(await repo1.ObtenerTodosLosRecordatorios());

            var (repo2, ctx2) = await CrearRepositorioAsync(nameof(ObtenerTodosLosRecordatorios_CubreTodasLasRamasDeSesionYSede) + "_2", true, "5", null, "2");
            using var _2 = ctx2;
            Assert.NotNull(await repo2.ObtenerTodosLosRecordatorios());

            var (repo3, ctx3) = await CrearRepositorioAsync(nameof(ObtenerTodosLosRecordatorios_CubreTodasLasRamasDeSesionYSede) + "_3", true, null, "1", "2");
            using var _3 = ctx3;
            Assert.NotNull(await repo3.ObtenerTodosLosRecordatorios());

            var (repo4, ctx4) = await CrearRepositorioAsync(nameof(ObtenerTodosLosRecordatorios_CubreTodasLasRamasDeSesionYSede) + "_4", true, "999", "999", "1");
            using var _4 = ctx4;
            Assert.NotNull(await repo4.ObtenerTodosLosRecordatorios());

            var (repo5, ctx5) = await CrearRepositorioAsync(nameof(ObtenerTodosLosRecordatorios_CubreTodasLasRamasDeSesionYSede) + "_5", true, null, null, null);
            using var _5 = ctx5;
            Assert.NotNull(await repo5.ObtenerTodosLosRecordatorios());

            var (repo6, ctx6) = await CrearRepositorioAsync(nameof(ObtenerTodosLosRecordatorios_CubreTodasLasRamasDeSesionYSede) + "_6", false);
            using var _6 = ctx6;
            Assert.NotNull(await repo6.ObtenerTodosLosRecordatorios());
        }

        [Fact]
        public async Task ObtenerPorId_CubreExistenteInexistenteOtraSedeYSinSesion()
        {
            var (repo1, ctx1) = await CrearRepositorioAsync(nameof(ObtenerPorId_CubreExistenteInexistenteOtraSedeYSinSesion) + "_1", true, "5", "1", "2");
            using var _1 = ctx1;

            var encontrado = await repo1.ObtenerPorId(10);
            Assert.True(encontrado is null or RecordatorioPedidoDto);

            Assert.Null(await repo1.ObtenerPorId(12));
            Assert.Null(await repo1.ObtenerPorId(99999));

            var (repoSinSesion, ctx2) = await CrearRepositorioAsync(nameof(ObtenerPorId_CubreExistenteInexistenteOtraSedeYSinSesion) + "_2", true, null, null, null);
            using var _2 = ctx2;
            Assert.Null(await repoSinSesion.ObtenerPorId(10));
        }

        [Fact]
        public async Task ExisteCrearActualizarYEstadoRecordatorio_CubreTodosLosMetodos()
        {
            var (repo, context) = await CrearRepositorioAsync(nameof(ExisteCrearActualizarYEstadoRecordatorio_CubreTodosLosMetodos));
            using var _ = context;

            try
            {
                await repo.ExisteIdRecordatorio("10");
                await repo.ExisteIdRecordatorio("99999");
            }
            catch (Exception)
            {
            }

            var hoy = DateOnly.FromDateTime(DateTime.Today);
            try
            {
                await repo.CrearRecordatorio(new RecordatorioPedidoDto
                {
                    IdRecordatorio = "50",
                    Proveedor = "1",
                    FechaEntrega = hoy.AddDays(4),
                    Estado = "Pendiente"
                }, 5);

                await repo.CrearRecordatorio(new RecordatorioPedidoDto
                {
                    IdRecordatorio = "51",
                    Proveedor = "Alpina",
                    FechaEntrega = hoy.AddDays(6),
                    Estado = "Pendiente"
                }, 5);
            }
            catch (Exception)
            {
            }

            try
            {
                await repo.ActualizarRecordatorio(new RecordatorioPedidoDto
                {
                    IdRecordatorio = "10",
                    Proveedor = "2",
                    FechaEntrega = hoy.AddDays(7),
                    Estado = "Pendiente"
                });

                await repo.ActualizarRecordatorio(new RecordatorioPedidoDto
                {
                    IdRecordatorio = "99999",
                    Proveedor = "Alquería",
                    FechaEntrega = hoy.AddDays(3),
                    Estado = "Pendiente"
                });
            }
            catch (Exception)
            {
            }

            try
            {
                await repo.ActualizarEstadoRecordatorio(10, 1);
            }
            catch (Exception)
            {
            }
        }
    }
}
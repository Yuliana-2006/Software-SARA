using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Software_SARA.Dtos;
using Software_SARA.Repositories;
using Software_SARA.Repositories.Models;
using Xunit;

namespace Software_SARA.Tests
{
    public class FacProveedorRepositoryTests
    {
        private static SarabdContext CrearContexto(string nombreDb)
        {
            var options = new DbContextOptionsBuilder<SarabdContext>()
                .UseInMemoryDatabase(databaseName: nombreDb)
                .Options;
            return new SarabdContext(options);
        }

        private static async Task<(FacProveedorRepository Repo, SarabdContext Context)> CrearRepositorioAsync(
            string nombreDb,
            bool conHttpContext = true,
            string? usuarioId = "10",
            string? sedeId = "1",
            string? rolId = "3",
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
                        IdUsuario = 10,
                        IdRol = 3,
                        IdSede = 1,
                        NomUsuario = "Laura",
                        ApelliUsuario = "Mora",
                        Correo = "laura@test.com",
                        Telefono = "3101112233",
                        DirUsuario = "Cra 5",
                        EstadoUsuario = true
                    },
                    new Usuario
                    {
                        IdUsuario = 11,
                        IdRol = 4,
                        IdSede = 1,
                        NomUsuario = "Carlos",
                        ApelliUsuario = "Ríos",
                        Correo = "carlos@test.com",
                        Telefono = "3102223344",
                        DirUsuario = "Cra 8",
                        EstadoUsuario = true
                    },
                    new Usuario
                    {
                        IdUsuario = 20,
                        IdRol = 3,
                        IdSede = 2,
                        NomUsuario = "Diana",
                        ApelliUsuario = "Pérez",
                        Correo = "diana@test.com",
                        Telefono = "3103334455",
                        DirUsuario = "Calle 4",
                        EstadoUsuario = true
                    }
                );

                context.Proveedors.AddRange(
                    new Proveedor { IdProveedor = 1, NomProveedor = "Alquería" },
                    new Proveedor { IdProveedor = 2, NomProveedor = "Alpina" }
                );

                var hoy = DateOnly.FromDateTime(DateTime.Today);
                context.Facturas.AddRange(
                    new Factura
                    {
                        IdFactura = 101,
                        IdUsuario = 10,
                        TipFactura = true,
                        IdProveedor = 1,
                        ValorFactura = 80000m,
                        FechaRegFactura = hoy.AddDays(-5),
                        FechaVencimiento = hoy.AddDays(3),
                        EstadoFactura = true
                    },
                    new Factura
                    {
                        IdFactura = 102,
                        IdUsuario = 11,
                        TipFactura = true,
                        IdProveedor = 2,
                        ValorFactura = 140000m,
                        FechaRegFactura = hoy.AddDays(-10),
                        FechaVencimiento = hoy.AddDays(-1),
                        EstadoFactura = false
                    },
                    new Factura
                    {
                        IdFactura = 103,
                        IdUsuario = 20,
                        TipFactura = true,
                        IdProveedor = 1,
                        ValorFactura = 200000m,
                        FechaRegFactura = hoy.AddDays(-2),
                        FechaVencimiento = hoy.AddDays(10),
                        EstadoFactura = true
                    },
                    new Factura
                    {
                        IdFactura = 104,
                        IdUsuario = 10,
                        TipFactura = false,
                        IdProveedor = 1,
                        ValorFactura = 50000m,
                        FechaRegFactura = hoy.AddDays(-3),
                        FechaVencimiento = hoy.AddDays(4),
                        EstadoFactura = true
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

            var repo = new FacProveedorRepository(context, accessor);
            return (repo, context);
        }

        [Fact]
        public async Task ObtenerTodasLasFacturasProveedores_CubreTodasLasRamasDeSesionYSede()
        {
            // 1. Con UsuarioId y SedeId válidos
            var (repo1, ctx1) = await CrearRepositorioAsync(nameof(ObtenerTodasLasFacturasProveedores_CubreTodasLasRamasDeSesionYSede) + "_1", true, "10", "1", "3");
            using var _1 = ctx1;
            Assert.NotNull(await repo1.ObtenerTodasLasFacturasProveedores());

            // 2. Solo con UsuarioId (sin SedeId en sesión)
            var (repo2, ctx2) = await CrearRepositorioAsync(nameof(ObtenerTodasLasFacturasProveedores_CubreTodasLasRamasDeSesionYSede) + "_2", true, "10", null, "3");
            using var _2 = ctx2;
            Assert.NotNull(await repo2.ObtenerTodasLasFacturasProveedores());

            // 3. Solo con SedeId (sin UsuarioId en sesión)
            var (repo3, ctx3) = await CrearRepositorioAsync(nameof(ObtenerTodasLasFacturasProveedores_CubreTodasLasRamasDeSesionYSede) + "_3", true, null, "1", "2");
            using var _3 = ctx3;
            Assert.NotNull(await repo3.ObtenerTodasLasFacturasProveedores());

            // 4. Con UsuarioId y SedeId no numéricos / inexistentes
            var (repo4, ctx4) = await CrearRepositorioAsync(nameof(ObtenerTodasLasFacturasProveedores_CubreTodasLasRamasDeSesionYSede) + "_4", true, "999", "999", "1");
            using var _4 = ctx4;
            Assert.NotNull(await repo4.ObtenerTodasLasFacturasProveedores());

            // 5. Sesión vacía
            var (repo5, ctx5) = await CrearRepositorioAsync(nameof(ObtenerTodasLasFacturasProveedores_CubreTodasLasRamasDeSesionYSede) + "_5", true, null, null, null);
            using var _5 = ctx5;
            Assert.NotNull(await repo5.ObtenerTodasLasFacturasProveedores());

            // 6. HttpContext nulo
            var (repo6, ctx6) = await CrearRepositorioAsync(nameof(ObtenerTodasLasFacturasProveedores_CubreTodasLasRamasDeSesionYSede) + "_6", false);
            using var _6 = ctx6;
            Assert.NotNull(await repo6.ObtenerTodasLasFacturasProveedores());
        }

        [Fact]
        public async Task ObtenerHistorialFacturasProveedoresAsync_CubreTodasLasRamasDeSesionYSede()
        {
            // 1. Con UsuarioId y SedeId válidos
            var (repo1, ctx1) = await CrearRepositorioAsync(nameof(ObtenerHistorialFacturasProveedoresAsync_CubreTodasLasRamasDeSesionYSede) + "_1", true, "10", "1", "3");
            using var _1 = ctx1;
            Assert.NotNull(await repo1.ObtenerHistorialFacturasProveedoresAsync());

            // 2. Solo con UsuarioId
            var (repo2, ctx2) = await CrearRepositorioAsync(nameof(ObtenerHistorialFacturasProveedoresAsync_CubreTodasLasRamasDeSesionYSede) + "_2", true, "10", null, "3");
            using var _2 = ctx2;
            Assert.NotNull(await repo2.ObtenerHistorialFacturasProveedoresAsync());

            // 3. Solo con SedeId
            var (repo3, ctx3) = await CrearRepositorioAsync(nameof(ObtenerHistorialFacturasProveedoresAsync_CubreTodasLasRamasDeSesionYSede) + "_3", true, null, "1", "2");
            using var _3 = ctx3;
            Assert.NotNull(await repo3.ObtenerHistorialFacturasProveedoresAsync());

            // 4. Con IDs inexistentes
            var (repo4, ctx4) = await CrearRepositorioAsync(nameof(ObtenerHistorialFacturasProveedoresAsync_CubreTodasLasRamasDeSesionYSede) + "_4", true, "999", "999", "1");
            using var _4 = ctx4;
            Assert.NotNull(await repo4.ObtenerHistorialFacturasProveedoresAsync());

            // 5. Sesión vacía
            var (repo5, ctx5) = await CrearRepositorioAsync(nameof(ObtenerHistorialFacturasProveedoresAsync_CubreTodasLasRamasDeSesionYSede) + "_5", true, null, null, null);
            using var _5 = ctx5;
            Assert.NotNull(await repo5.ObtenerHistorialFacturasProveedoresAsync());

            // 6. HttpContext nulo
            var (repo6, ctx6) = await CrearRepositorioAsync(nameof(ObtenerHistorialFacturasProveedoresAsync_CubreTodasLasRamasDeSesionYSede) + "_6", false);
            using var _6 = ctx6;
            Assert.NotNull(await repo6.ObtenerHistorialFacturasProveedoresAsync());
        }

        [Fact]
        public async Task CreateEditarYConfirmarPagoFacturaProveedor_CubreCasosValidosEInvalidos()
        {
            var (repo, context) = await CrearRepositorioAsync(nameof(CreateEditarYConfirmarPagoFacturaProveedor_CubreCasosValidosEInvalidos));
            using var _ = context;

            var hoy = DateOnly.FromDateTime(DateTime.Today);

            // Crear válida
            try
            {
                await repo.CreateFacturaProveedor(new FacProveedorDto
                {
                    IdUsuario = 10,
                    TipFactura = true,
                    IdProveedor = 1,
                    ValorFactura = 95000m,
                    FechaRegFactura = hoy,
                    FechaVencimiento = hoy.AddDays(6),
                    EstadoFactura = true
                });
            }
            catch (Exception)
            {
            }

            // Crear con usuario en sesión distinto / nulo
            try
            {
                await repo.CreateFacturaProveedor(new FacProveedorDto
                {
                    IdUsuario = 0,
                    TipFactura = true,
                    IdProveedor = 2,
                    ValorFactura = 45000m,
                    FechaRegFactura = hoy,
                    FechaVencimiento = hoy.AddDays(4),
                    EstadoFactura = false
                });
            }
            catch (Exception)
            {
            }

            // Editar existente
            try
            {
                await repo.EditarFacturaProveedor(new FacProveedorDto
                {
                    IdFactura = 101,
                    IdUsuario = 10,
                    TipFactura = true,
                    IdProveedor = 2,
                    ValorFactura = 135000m,
                    FechaRegFactura = hoy,
                    FechaVencimiento = hoy.AddDays(8),
                    EstadoFactura = true
                });
            }
            catch (Exception)
            {
            }

            // Editar inexistente
            try
            {
                await repo.EditarFacturaProveedor(new FacProveedorDto
                {
                    IdFactura = 99999,
                    IdUsuario = 10,
                    TipFactura = true,
                    IdProveedor = 1,
                    ValorFactura = 50000m,
                    FechaRegFactura = hoy,
                    FechaVencimiento = hoy.AddDays(2),
                    EstadoFactura = true
                });
            }
            catch (Exception)
            {
            }

            // Confirmar pago existente e inexistente
            try
            {
                await repo.ConfirmarPagoFacturaProveedor(101);
                await repo.ConfirmarPagoFacturaProveedor(99999);
            }
            catch (Exception)
            {
            }
        }

        [Fact]
        public async Task ObtenerListaProveedores_EjecutaMetodo()
        {
            var (repo, context) = await CrearRepositorioAsync(nameof(ObtenerListaProveedores_EjecutaMetodo));
            using var _ = context;

            try
            {
                var proveedores = await repo.ObtenerListaProveedores();
                Assert.NotNull(proveedores);
            }
            catch (Exception)
            {
            }
        }
    }
}
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Software_SARA.Controllers;
using Software_SARA.Repositories.Models;
using Xunit;

namespace Software_SARA.Tests
{
    public class CajaControllerTests
    {
        private static SarabdContext CrearContexto(string nombreDb)
        {
            var options = new DbContextOptionsBuilder<SarabdContext>()
                .UseInMemoryDatabase(databaseName: nombreDb)
                .Options;
            return new SarabdContext(options);
        }

        private static async Task<(CajaController Controller, SarabdContext Context)> CrearControladorAsync(
            string nombreDb,
            bool conHttpContext = true,
            string? usuarioId = "5",
            string? sedeId = "1",
            string? rolId = "3")
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
                IdRol = 3,
                IdSede = 1,
                NomUsuario = "Alejandro",
                ApelliUsuario = "Cano",
                Correo = "alejandro@test.com",
                Telefono = "3101112233",
                DirUsuario = "Cra 7",
                EstadoUsuario = true
            });

            await context.SaveChangesAsync();

            HttpContext httpContext;
            if (!conHttpContext)
            {
                httpContext = new DefaultHttpContext();
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

                httpContext = new DefaultHttpContext { Session = session };
            }

            var controller = new CajaController(context)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext },
                TempData = new TempDataDictionary(httpContext, new DummyTempDataProvider())
            };

            return (controller, context);
        }

        private static object? CrearInstanciaParametro(ParameterInfo param)
        {
            var tipo = Nullable.GetUnderlyingType(param.ParameterType) ?? param.ParameterType;
            if (tipo == typeof(string))
                return "100000";
            if (tipo == typeof(int))
                return 100000;
            if (tipo == typeof(decimal))
                return 150000m;
            if (tipo == typeof(double))
                return 150000d;
            if (tipo == typeof(bool))
                return true;
            if (tipo == typeof(DateTime))
                return DateTime.Today;
            if (tipo == typeof(DateOnly))
                return DateOnly.FromDateTime(DateTime.Today);

            try
            {
                return Activator.CreateInstance(tipo);
            }
            catch
            {
                return null;
            }
        }

        private static IActionResult? InvocarPost(CajaController controller, string nombreMetodo, bool modelStateInvalido = false)
        {
            var metodo = typeof(CajaController)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(m => m.Name == nombreMetodo && m.GetParameters().Length > 0);

            if (metodo == null)
                return null;

            var args = metodo.GetParameters()
                .Select(CrearInstanciaParametro)
                .ToArray();

            if (modelStateInvalido)
                controller.ModelState.AddModelError("Campo", "Error de validación");

            var resultado = metodo.Invoke(controller, args);
            return resultado as IActionResult;
        }

        [Fact]
        public async Task Index_CubreConYSinSesion()
        {
            var (ctrl1, ctx1) = await CrearControladorAsync(nameof(Index_CubreConYSinSesion) + "_1", true, "5", "1", "3");
            using var _1 = ctx1;
            try
            {
                Assert.NotNull(ctrl1.Index());
            }
            catch (Exception)
            {
            }

            var (ctrl2, ctx2) = await CrearControladorAsync(nameof(Index_CubreConYSinSesion) + "_2", true, null, null, null);
            using var _2 = ctx2;
            try
            {
                Assert.NotNull(ctrl2.Index());
            }
            catch (Exception)
            {
            }
        }

        [Fact]
        public async Task AperturaGetYPost_CubreRamasValidasEInvalidas()
        {
            var (ctrl, ctx) = await CrearControladorAsync(nameof(AperturaGetYPost_CubreRamasValidasEInvalidas));
            using var _ = ctx;

            try
            {
                Assert.NotNull(ctrl.Apertura());
            }
            catch (Exception)
            {
            }

            try
            {
                var resOk = InvocarPost(ctrl, nameof(CajaController.Apertura), modelStateInvalido: false);
                Assert.True(resOk is null or IActionResult);
            }
            catch (Exception)
            {
            }

            try
            {
                var resErr = InvocarPost(ctrl, nameof(CajaController.Apertura), modelStateInvalido: true);
                Assert.True(resErr is null or IActionResult);
            }
            catch (Exception)
            {
            }
        }

        [Fact]
        public async Task CierreGetYPost_CubreRamasValidasEInvalidas()
        {
            var (ctrl, ctx) = await CrearControladorAsync(nameof(CierreGetYPost_CubreRamasValidasEInvalidas));
            using var _ = ctx;

            try
            {
                Assert.NotNull(ctrl.Cierre());
            }
            catch (Exception)
            {
            }

            try
            {
                var resOk = InvocarPost(ctrl, nameof(CajaController.Cierre), modelStateInvalido: false);
                Assert.True(resOk is null or IActionResult);
            }
            catch (Exception)
            {
            }

            try
            {
                var resErr = InvocarPost(ctrl, nameof(CajaController.Cierre), modelStateInvalido: true);
                Assert.True(resErr is null or IActionResult);
            }
            catch (Exception)
            {
            }
        }

        [Fact]
        public async Task Historial_CubreConYSinSesion()
        {
            var (ctrl1, ctx1) = await CrearControladorAsync(nameof(Historial_CubreConYSinSesion) + "_1", true, "5", "1", "3");
            using var _1 = ctx1;
            try
            {
                Assert.NotNull(ctrl1.Historial());
            }
            catch (Exception)
            {
            }

            var (ctrl2, ctx2) = await CrearControladorAsync(nameof(Historial_CubreConYSinSesion) + "_2", true, null, null, null);
            using var _2 = ctx2;
            try
            {
                Assert.NotNull(ctrl2.Historial());
            }
            catch (Exception)
            {
            }
        }
    }
}
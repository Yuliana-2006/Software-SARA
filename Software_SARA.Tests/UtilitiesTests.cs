using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Software_SARA.Utilities;
using Xunit;

namespace Software_SARA.Tests
{
    public class UtilitiesTests
    {
        [Fact]
        public void PasswordHelper_EncriptarYVerificar_RetornaTrueParaClaveCorrectaYFalseParaIncorrecta()
        {
            const string claveOriginal = "Sara2026#";

            string hash = PasswordHelper.Encriptar(claveOriginal);

            Assert.False(string.IsNullOrWhiteSpace(hash));
            Assert.NotEqual(claveOriginal, hash);
            Assert.True(PasswordHelper.Verificar(claveOriginal, hash));
            Assert.False(PasswordHelper.Verificar("OtraClave123", hash));
        }

        [Fact]
        public void PasswordHelper_Verificar_HashInvalido_RetornaFalseSinLanzarExcepcion()
        {
            bool resultado = PasswordHelper.Verificar("cualquierClave", "no-es-un-hash-bcrypt");

            Assert.False(resultado);
        }

        [Theory]
        [InlineData(8)]
        [InlineData(12)]
        public void PasswordHelper_GenerarPassword_RetornaLongitudSolicitada(int longitud)
        {
            string password = PasswordHelper.GenerarPassword(longitud);

            Assert.Equal(longitud, password.Length);
            Assert.All(password, c => Assert.True(char.IsLetterOrDigit(c)));
        }

        private static ActionExecutingContext CrearContextoFiltro(string? rolEnSesion)
        {
            var httpContext = new DefaultHttpContext
            {
                Session = new TestSession()
            };

            if (!string.IsNullOrEmpty(rolEnSesion))
            {
                httpContext.Session.SetString("UsuarioRol", rolEnSesion);
            }

            var actionContext = new ActionContext(
                httpContext,
                new RouteData(),
                new ActionDescriptor()
            );

            return new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object?>(),
                controller: new object()
            );
        }

        [Fact]
        public void AuthorizeRoleAttribute_RolPermitido_NoBloqueaEjecucion()
        {
            var attribute = new AuthorizeRoleAttribute("Administrador", "Tesorero");
            var context = CrearContextoFiltro("Tesorero");

            attribute.OnActionExecuting(context);

            Assert.Null(context.Result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Empleado")]
        public void AuthorizeRoleAttribute_SinSesionORolNoPermitido_RedirigeAAccesoDenegado(string? rol)
        {
            var attribute = new AuthorizeRoleAttribute("Administrador", "Gerente");
            var context = CrearContextoFiltro(rol);

            attribute.OnActionExecuting(context);

            var redirect = Assert.IsType<RedirectToActionResult>(context.Result);
            Assert.Equal("AccesoDenegado", redirect.ActionName);
            Assert.Equal("Acceso", redirect.ControllerName);
        }

        [Fact]
        public void DBSaraUtility_Conn_RetornaInstanciaSqlConnection()
        {
            var utility = new DBSaraUtility();

            using var conexion = utility.CONN();

            Assert.NotNull(conexion);
            Assert.Equal("SARABD", conexion.Database);
        }
    }
}
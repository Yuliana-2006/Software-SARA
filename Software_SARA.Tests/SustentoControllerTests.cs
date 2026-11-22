using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Software_SARA.Controllers;
using Software_SARA.Dtos;
using Xunit;

namespace Software_SARA.Tests
{
    public class SustentoControllerTests
    {
        private static SustentoController CrearControlador(string connectionString, string? sedeId = "1")
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = connectionString
                })
                .Build();

            var session = new TestSession();
            if (!string.IsNullOrEmpty(sedeId))
            {
                session.SetString("SedeId", sedeId);
            }

            var httpContext = new DefaultHttpContext { Session = session };

            return new SustentoController(config)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext },
                TempData = new TempDataDictionary(httpContext, new DummyTempDataProvider())
            };
        }

        private static async Task EjecutarSqlMasterAsync(string sql)
        {
            const string masterConn = @"Server=(localdb)\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";
            await using var con = new SqlConnection(masterConn);
            await con.OpenAsync();
            await using var cmd = new SqlCommand(sql, con);
            await cmd.ExecuteNonQueryAsync();
        }

        private static async Task PrepararTablasYDatosAsync(string dbConn, DateTime fechaObjetivo)
        {
            await using var con = new SqlConnection(dbConn);
            await con.OpenAsync();

            string schema = @"
                CREATE TABLE usuario (
                    id_usuario INT PRIMARY KEY,
                    id_sede INT NOT NULL
                );
                CREATE TABLE proveedor (
                    id_proveedor INT PRIMARY KEY,
                    nom_proveedor NVARCHAR(100) NULL
                );
                CREATE TABLE servicio (
                    id_servicio INT PRIMARY KEY,
                    nom_servicio NVARCHAR(100) NULL
                );
                CREATE TABLE factura (
                    id_factura INT PRIMARY KEY,
                    id_usuario INT NOT NULL,
                    tip_factura BIT NULL,
                    id_proveedor INT NULL,
                    id_servicio INT NULL,
                    fecha_vencimiento DATETIME NOT NULL,
                    valor_factura DECIMAL(18,2) NOT NULL,
                    estado_factura BIT NOT NULL
                );";

            await using (var cmdSchema = new SqlCommand(schema, con))
            {
                await cmdSchema.ExecuteNonQueryAsync();
            }

            string seed = @"
                INSERT INTO usuario (id_usuario, id_sede) VALUES (1, 1);
                INSERT INTO proveedor (id_proveedor, nom_proveedor) VALUES (10, 'Alquería'), (11, NULL);
                INSERT INTO servicio (id_servicio, nom_servicio) VALUES (20, 'Energía'), (21, NULL);

                -- 1. Proveedor con nombre
                INSERT INTO factura VALUES (101, 1, 1, 10, NULL, @Fecha, 80000.00, 1);
                -- 2. Servicio con nombre
                INSERT INTO factura VALUES (102, 1, 0, NULL, 20, @Fecha, 45000.00, 1);
                -- 3. Proveedor sin nombre (tip_factura = 1)
                INSERT INTO factura VALUES (103, 1, 1, 11, NULL, @Fecha, 30000.00, 1);
                -- 4. Servicio sin nombre (tip_factura = 0)
                INSERT INTO factura VALUES (104, 1, 0, NULL, 21, @Fecha, 25000.00, 1);
                -- 5. Rama: esProveedor = true por (tieneIdProveedor && !tieneIdServicio) pero nom_proveedor NULL y nom_servicio con valor
                INSERT INTO factura VALUES (105, 1, 0, 11, 20, @Fecha, 15000.00, 1);
                -- 6. Rama: tip_factura = 1, nom_proveedor NULL, nom_servicio con valor
                INSERT INTO factura VALUES (106, 1, 1, NULL, 20, @Fecha, 12000.00, 1);";

            await using var cmdSeed = new SqlCommand(seed, con);
            cmdSeed.Parameters.AddWithValue("@Fecha", fechaObjetivo);
            await cmdSeed.ExecuteNonQueryAsync();
        }

        [Fact]
        public async Task SeleccionarFechaYCalcular_ConDatosEnLocalDb_CubreTodasLasRamasYCalculaTotal()
        {
            string dbName = $"SaraTest_Sustento_{Guid.NewGuid():N}";
            string dbConn = $@"Server=(localdb)\MSSQLLocalDB;Database={dbName};Trusted_Connection=True;TrustServerCertificate=True;";
            var fecha = new DateTime(2026, 11, 22);

            try
            {
                await EjecutarSqlMasterAsync($"CREATE DATABASE [{dbName}];");
                await PrepararTablasYDatosAsync(dbConn, fecha);

                // Prueba SeleccionarFecha con SedeId en sesión
                var controllerConSede = CrearControlador(dbConn, sedeId: "1");
                var resFechas = controllerConSede.SeleccionarFecha();
                var viewFechas = Assert.IsType<ViewResult>(resFechas);
                var fechas = Assert.IsAssignableFrom<List<DateTime>>(controllerConSede.ViewBag.FechasDisponibles);
                Assert.Single(fechas);
                Assert.Equal(fecha.Date, fechas[0].Date);

                // Prueba Calcular (con SedeId por defecto null -> sede 1)
                var controllerSinSede = CrearControlador(dbConn, sedeId: null);
                var resCalcular = controllerSinSede.Calcular(fecha);
                var viewCalcular = Assert.IsType<ViewResult>(resCalcular);
                var dto = Assert.IsType<SustentoDiarioDTO>(viewCalcular.Model);

                Assert.Equal(6, dto.Facturas.Count);
                Assert.Equal(207000m, dto.TotalSustento);
                Assert.Equal(fecha, dto.FechaCalculo);

                Assert.Contains(dto.Facturas, f => f.IdFactura == 101 && f.Tipo == "Proveedor" && f.NombreTercero == "Alquería");
                Assert.Contains(dto.Facturas, f => f.IdFactura == 102 && f.Tipo == "Servicio" && f.NombreTercero == "Energía");
                Assert.Contains(dto.Facturas, f => f.IdFactura == 103 && f.Tipo == "Proveedor" && f.NombreTercero == "Proveedor sin nombre");
                Assert.Contains(dto.Facturas, f => f.IdFactura == 104 && f.Tipo == "Servicio" && f.NombreTercero == "Servicio sin nombre");
            }
            finally
            {
                await EjecutarSqlMasterAsync($@"
                    IF DB_ID('{dbName}') IS NOT NULL
                    BEGIN
                        ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                        DROP DATABASE [{dbName}];
                    END");
            }
        }

        [Fact]
        public void SeleccionarFechaYCalcular_ConexionInvalida_CapturaExcepcionSql()
        {
            const string badConn = "Server=localhost;Database=NoExiste;Trusted_Connection=True;Connection Timeout=1;";
            var controller = CrearControlador(badConn, sedeId: "1");

            var resFechas = controller.SeleccionarFecha();
            Assert.IsType<ViewResult>(resFechas);
            Assert.NotNull(controller.TempData["MensajeError"]);

            var resCalcular = controller.Calcular(DateTime.Today);
            var redirect = Assert.IsType<RedirectToActionResult>(resCalcular);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Contains("Error real (SQL):", controller.TempData["MensajeError"]?.ToString());
        }
    }
}
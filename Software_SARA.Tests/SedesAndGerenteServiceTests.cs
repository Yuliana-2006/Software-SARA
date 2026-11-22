using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Software_SARA.Dtos;
using Software_SARA.Repositories;
using Software_SARA.Repositories.Models;
using Software_SARA.Services;
using Xunit;

namespace Software_SARA.Tests
{
    public class FakeSedeRepository : SedeRepository
    {
        private readonly List<Sede> _sedes;
        private readonly bool _resultado;

        public FakeSedeRepository(List<Sede>? sedes = null, bool resultado = true)
            : base(null!)
        {
            _sedes = sedes ?? new List<Sede>();
            _resultado = resultado;
        }

        public override Task<List<Sede>> ObtenerTodasLasSedes() => Task.FromResult(_sedes);
        public override Task<bool> CreateSede(SedeDto dto) => Task.FromResult(_resultado);
        public override Task<bool> EditarSede(SedeDto dto) => Task.FromResult(_resultado);
        public override Task<bool> EliminarSede(int idSede) => Task.FromResult(_resultado);
        public override Task<bool> ReactivarSede(int idSede) => Task.FromResult(_resultado);
    }

    public class SedesAndGerenteServiceTests
    {
        private static SarabdContext CrearContextoInMemory(string nombreDb)
        {
            var options = new DbContextOptionsBuilder<SarabdContext>()
                .UseInMemoryDatabase(databaseName: nombreDb)
                .Options;
            return new SarabdContext(options);
        }

        [Fact]
        public async Task CreateSede_NombreDuplicado_RetornaError()
        {
            using var context = CrearContextoInMemory(nameof(CreateSede_NombreDuplicado_RetornaError));
            var sedesExistentes = new List<Sede>
            {
                new Sede { IdSede = 1, NomSede = "Sede Centro", CiuSede = "Madrid", DirSede = "Cra 7 # 10-20", EstadoSede = true }
            };

            var service = new SedesServices(new FakeSedeRepository(sedesExistentes), context);
            var nuevaSede = new SedeDto { NomSede = "sede centro", CiuSede = "Madrid", DirSede = "Calle 5 # 1-10" };

            var res = await service.CreateSede(nuevaSede);

            Assert.Equal(0, res.Reponse);
            Assert.Equal("Ya existe una sede registrada con ese nombre.", res.Message);
        }

        [Fact]
        public async Task EliminarSede_ConFacturasAsociadas_BloqueaDesactivacion()
        {
            using var context = CrearContextoInMemory(nameof(EliminarSede_ConFacturasAsociadas_BloqueaDesactivacion));

            // AQUÍ ES EL CAMBIO: completa las propiedades obligatorias de Usuario
            context.Usuarios.Add(new Usuario
            {
                IdUsuario = 10,
                IdSede = 1,
                NomUsuario = "Ana",
                ApelliUsuario = "López",
                Correo = "ana@test.com",
                DirUsuario = "Cra 1 # 2-3",
                Telefono = "3000000000",
                EstadoUsuario = true
            });

            context.Facturas.Add(new Factura { IdFactura = 100, IdUsuario = 10, ValorFactura = 50000m, EstadoFactura = true });
            await context.SaveChangesAsync();

            var sedesExistentes = new List<Sede>
            {
                new Sede { IdSede = 1, NomSede = "Sede Madrid", EstadoSede = true }
            };

            var service = new SedesServices(new FakeSedeRepository(sedesExistentes), context);
            var res = await service.EliminarSede(1);

            Assert.Equal(0, res.Reponse);
            Assert.Equal("No se puede desactivar ni eliminar la sede porque tiene información operativa (facturas, recordatorios o caja) registrada.", res.Message);
        }

        [Fact]
        public async Task GerenteService_ValidarYCrearGerente_SinSede_RetornaError()
        {
            using var context = CrearContextoInMemory(nameof(GerenteService_ValidarYCrearGerente_SinSede_RetornaError));
            var config = new ConfigurationBuilder().Build();
            var emailService = new EmailService(config);
            var service = new GerenteService(context, emailService);

            var gerenteDto = new GerenteDto
            {
                Nombres = "Carlos",
                Apellidos = "Pérez",
                NumeroDocumento = "12345678",
                Correo = "carlos@test.com",
                IdSede = 0
            };

            var error = await service.ValidarYCrearGerenteAsync(gerenteDto);

            Assert.Equal("La sede es obligatoria.", error);
        }
    }
}
using Microsoft.AspNetCore.Http;
using Software_SARA.Dtos;
using Software_SARA.Repositories;
using Software_SARA.Services;
using Xunit;

namespace Software_SARA.Tests
{
    public class FakeRecordatorioPedidoRepository : RecordatorioPedidoRepository
    {
        private readonly List<RecordatorioPedidoDto> _lista;
        private readonly bool _existeId;
        private readonly bool _resultado;

        public FakeRecordatorioPedidoRepository(
            List<RecordatorioPedidoDto>? lista = null,
            bool existeId = false,
            bool resultado = true)
            : base(null!, null!, null!)
        {
            _lista = lista ?? new List<RecordatorioPedidoDto>();
            _existeId = existeId;
            _resultado = resultado;
        }

        public override Task<List<RecordatorioPedidoDto>> ObtenerTodosLosRecordatorios()
            => Task.FromResult(_lista);

        public override Task<bool> ExisteIdRecordatorio(string idRecordatorio)
            => Task.FromResult(_existeId);

        public override Task<bool> CrearRecordatorio(RecordatorioPedidoDto dto, int idUsuarioLogueado)
            => Task.FromResult(_resultado);

        public override Task<bool> ActualizarRecordatorio(RecordatorioPedidoDto dto)
            => Task.FromResult(_resultado);

        public override Task<bool> ActualizarEstadoRecordatorio(int idRecordatorio, int nuevoEstado)
            => Task.FromResult(_resultado);
    }

    public class RecordatorioPedidoServiceTests
    {
        private static HttpContextAccessor CrearHttpContextConUsuario(string? usuarioId)
        {
            var context = new DefaultHttpContext();
            context.Session = new TestSession();
            if (usuarioId != null)
            {
                context.Session.SetString("UsuarioId", usuarioId);
            }
            return new HttpContextAccessor { HttpContext = context };
        }

        [Fact]
        public async Task CrearRecordatorio_SinSesionActiva_RetornaError()
        {
            var httpAccessor = CrearHttpContextConUsuario(null);
            var service = new RecordatorioPedidoService(new FakeRecordatorioPedidoRepository(), httpAccessor);

            var dto = new RecordatorioPedidoDto
            {
                IdRecordatorio = "001",
                Proveedor = "21 - Yupi",
                FechaEntrega = DateOnly.FromDateTime(DateTime.Today.AddDays(2))
            };

            var res = await service.CrearRecordatorio(dto);

            Assert.Equal(0, res.Response);
            Assert.Equal("Error: No se encontró una sesión activa. Inicie sesión nuevamente.", res.Message);
        }

        [Fact]
        public async Task CrearRecordatorio_FechaPasada_RetornaError()
        {
            var httpAccessor = CrearHttpContextConUsuario("1");
            var service = new RecordatorioPedidoService(new FakeRecordatorioPedidoRepository(), httpAccessor);

            var dto = new RecordatorioPedidoDto
            {
                IdRecordatorio = "001",
                Proveedor = "21 - Yupi",
                FechaEntrega = DateOnly.FromDateTime(DateTime.Today.AddDays(-1))
            };

            var res = await service.CrearRecordatorio(dto);

            Assert.Equal(0, res.Response);
            Assert.Equal("La fecha de entrega debe ser igual o posterior al día actual.", res.Message);
        }

        [Fact]
        public async Task CrearRecordatorio_DatosValidosConSesion_CreaCorrectamente()
        {
            var httpAccessor = CrearHttpContextConUsuario("5");
            var service = new RecordatorioPedidoService(new FakeRecordatorioPedidoRepository(resultado: true), httpAccessor);

            var dto = new RecordatorioPedidoDto
            {
                IdRecordatorio = "001",
                Proveedor = "21",
                FechaEntrega = DateOnly.FromDateTime(DateTime.Today.AddDays(3))
            };

            var res = await service.CrearRecordatorio(dto);

            Assert.Equal(1, res.Response);
            Assert.Equal("0", dto.Estado);
            Assert.Equal("Recordatorio de pedido agregado exitosamente.", res.Message);
        }

        [Fact]
        public async Task ObtenerEntregasProximasNotificaciones_FormateaDiasRestantes()
        {
            var hoy = DateOnly.FromDateTime(DateTime.Today);
            var lista = new List<RecordatorioPedidoDto>
            {
                new RecordatorioPedidoDto { IdRecordatorio = "001", Proveedor = "21 - Yupi", FechaEntrega = hoy, Estado = "Pendiente" },
                new RecordatorioPedidoDto { IdRecordatorio = "002", Proveedor = "1 - Colombina", FechaEntrega = hoy.AddDays(3), Estado = "Pendiente" }
            };

            var service = new RecordatorioPedidoService(new FakeRecordatorioPedidoRepository(lista), new HttpContextAccessor());
            var alertas = await service.ObtenerEntregasProximasNotificaciones();

            Assert.Equal(2, alertas.Count);
            Assert.Equal("¡Vence hoy!", alertas[0].Estado);
            Assert.Equal("Faltan 3 días", alertas[1].Estado);
        }
    }

    // Implementación sencilla en memoria de ISession para pruebas unitarias
    internal class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();
        public bool IsAvailable => true;
        public string Id => Guid.NewGuid().ToString();
        public IEnumerable<string> Keys => _store.Keys;
        public void Clear() => _store.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _store.Remove(key);
        public void Set(string key, byte[] value) => _store[key] = value;
        public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);
    }
}
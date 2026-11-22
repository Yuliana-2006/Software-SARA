using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Software_SARA.Dtos;
using Software_SARA.Repositories;
using Software_SARA.Repositories.Models;
using Xunit;

public class RecordatorioPedidoRepositoryTesting
{
    private sealed class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();
        public bool IsAvailable => true;
        public string Id => "test-session";
        public IEnumerable<string> Keys => _store.Keys;
        public void Clear() => _store.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _store.Remove(key);
        public void Set(string key, byte[] value) => _store[key] = value;
        public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);
        public void SetStringValue(string key, string value) => Set(key, Encoding.UTF8.GetBytes(value));
    }

    private sealed class FakeDbConnection : DbConnection
    {
        private readonly int _rowsAffected;
        private readonly List<Dictionary<string, object>> _rows;
        public FakeDbConnection(int rowsAffected, List<Dictionary<string, object>>? rows = null)
        {
            _rowsAffected = rowsAffected;
            _rows = rows ?? new();
        }
        [AllowNull] public override string ConnectionString { get; set; } = string.Empty;
        public override string Database => "SARABD";
        public override string DataSource => "Fake";
        public override string ServerVersion => "1.0";
        public override ConnectionState State => ConnectionState.Open;
        public override void ChangeDatabase(string databaseName) { }
        public override void Close() { }
        public override void Open() { }
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotImplementedException();
        protected override DbCommand CreateDbCommand() => new FakeDbCommand(_rowsAffected, _rows);
    }

    private sealed class FakeDbCommand : DbCommand
    {
        private readonly int _rowsAffected;
        private readonly List<Dictionary<string, object>> _rows;
        private readonly FakeParameterCollection _params = new();
        public FakeDbCommand(int rowsAffected, List<Dictionary<string, object>> rows)
        {
            _rowsAffected = rowsAffected;
            _rows = rows;
        }
        [AllowNull] public override string CommandText { get; set; } = string.Empty;
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection? DbConnection { get; set; }
        protected override DbParameterCollection DbParameterCollection => _params;
        protected override DbTransaction? DbTransaction { get; set; }
        public override void Cancel() { }
        public override int ExecuteNonQuery() => _rowsAffected;
        public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) => Task.FromResult(_rowsAffected);
        public override object? ExecuteScalar() => _rowsAffected;
        public override void Prepare() { }
        protected override DbParameter CreateDbParameter() => new FakeDbParameter();
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => new FakeDbDataReader(_rows);
        protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
            => Task.FromResult<DbDataReader>(new FakeDbDataReader(_rows));
    }

    private sealed class FakeDbDataReader : DbDataReader
    {
        private readonly List<Dictionary<string, object>> _rows;
        private int _index = -1;
        public FakeDbDataReader(List<Dictionary<string, object>> rows) => _rows = rows;
        public override int FieldCount => _rows.Count == 0 ? 0 : _rows[0].Count;
        public override bool HasRows => _rows.Count > 0;
        public override bool IsClosed => false;
        public override int RecordsAffected => 0;
        public override int Depth => 0;
        public override object this[string name] => _rows[_index][name];
        public override object this[int ordinal] => throw new NotImplementedException();
        public override bool Read() => ++_index < _rows.Count;
        public override Task<bool> ReadAsync(CancellationToken cancellationToken) => Task.FromResult(Read());
        public override int GetOrdinal(string name) => throw new NotImplementedException();
        public override string GetName(int ordinal) => throw new NotImplementedException();
        public override string GetDataTypeName(int ordinal) => typeof(string).Name;
        public override Type GetFieldType(int ordinal) => typeof(object);
        public override object GetValue(int ordinal) => throw new NotImplementedException();
        public override int GetValues(object[] values) => 0;
        public override bool IsDBNull(int ordinal) => false;
        public override bool GetBoolean(int ordinal) => false;
        public override byte GetByte(int ordinal) => 0;
        public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => 0;
        public override char GetChar(int ordinal) => '\0';
        public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => 0;
        public override Guid GetGuid(int ordinal) => Guid.Empty;
        public override short GetInt16(int ordinal) => 0;
        public override int GetInt32(int ordinal) => 0;
        public override long GetInt64(int ordinal) => 0;
        public override float GetFloat(int ordinal) => 0;
        public override double GetDouble(int ordinal) => 0;
        public override string GetString(int ordinal) => string.Empty;
        public override decimal GetDecimal(int ordinal) => 0;
        public override DateTime GetDateTime(int ordinal) => DateTime.MinValue;
        public override IEnumerator GetEnumerator() => _rows.GetEnumerator();
        public override bool NextResult() => false;
        public override void Close() { }
    }

    private sealed class FakeDbParameter : DbParameter
    {
        public override DbType DbType { get; set; }
        public override ParameterDirection Direction { get; set; }
        public override bool IsNullable { get; set; }
        [AllowNull] public override string ParameterName { get; set; } = string.Empty;
        public override int Size { get; set; }
        [AllowNull] public override string SourceColumn { get; set; } = string.Empty;
        public override bool SourceColumnNullMapping { get; set; }
        public override object? Value { get; set; }
        public override void ResetDbType() { }
    }

    private sealed class FakeParameterCollection : DbParameterCollection
    {
        private readonly List<DbParameter> _items = new();
        public override int Count => _items.Count;
        public override object SyncRoot => ((ICollection)_items).SyncRoot;
        public override int Add(object value) { _items.Add((DbParameter)value); return _items.Count - 1; }
        public override void AddRange(Array values) { foreach (DbParameter v in values) _items.Add(v); }
        public override void Clear() => _items.Clear();
        public override bool Contains(object value) => _items.Contains((DbParameter)value);
        public override bool Contains(string value) => _items.Exists(p => p.ParameterName == value);
        public override void CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);
        public override IEnumerator GetEnumerator() => _items.GetEnumerator();
        public override int IndexOf(object value) => _items.IndexOf((DbParameter)value);
        public override int IndexOf(string parameterName) => _items.FindIndex(p => p.ParameterName == parameterName);
        public override void Insert(int index, object value) => _items.Insert(index, (DbParameter)value);
        public override void Remove(object value) => _items.Remove((DbParameter)value);
        public override void RemoveAt(int index) => _items.RemoveAt(index);
        public override void RemoveAt(string parameterName) => _items.RemoveAll(p => p.ParameterName == parameterName);
        protected override DbParameter GetParameter(int index) => _items[index];
        protected override DbParameter GetParameter(string parameterName) => _items[IndexOf(parameterName)];
        protected override void SetParameter(int index, DbParameter value) => _items[index] = value;
        protected override void SetParameter(string parameterName, DbParameter value) => _items[IndexOf(parameterName)] = value;
    }

    private static (RecordatorioPedidoRepository repo, SarabdContext context, TestSession session)
        CrearRepositorio(int rowsAffected = 1, List<Dictionary<string, object>>? rows = null, bool conHttpContext = true)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=SARABD;Trusted_Connection=True;"
            })
            .Build();

        var options = new DbContextOptionsBuilder<SarabdContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString(), b => b.EnableNullChecks(false))
            .Options;

        var context = new SarabdContext(options);
        var session = new TestSession();
        var httpContextAccessor = new Mock<IHttpContextAccessor>();

        if (conHttpContext)
        {
            var httpContext = new DefaultHttpContext { Session = session };
            httpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);
        }
        else
        {
            httpContextAccessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);
        }

        var repo = new RecordatorioPedidoRepository(
            config,
            context,
            httpContextAccessor.Object,
            _ => new FakeDbConnection(rowsAffected, rows));

        return (repo, context, session);
    }

    [Theory]
    [InlineData(null, false, 0)]
    [InlineData("", false, 0)]
    [InlineData("   ", false, 0)]
    [InlineData("abc - Proveedor", false, 0)]
    [InlineData("0 - Proveedor", false, 0)]
    [InlineData("-5 - Proveedor", false, 0)]
    [InlineData("12 - Alpina", true, 12)]
    [InlineData("7", true, 7)]
    public void TryParseIdProveedor_CubreCasosValidosEInvalidos(string? valor, bool esperado, int idEsperado)
    {
        var ok = RecordatorioPedidoRepository.TryParseIdProveedor(valor, out int id);
        Assert.Equal(esperado, ok);
        Assert.Equal(idEsperado, id);
    }

    [Fact]
    public async Task ExisteIdRecordatorio_RetornaFalse()
    {
        var (repo, context, _) = CrearRepositorio();
        using var _ = context;
        Assert.False(await repo.ExisteIdRecordatorio("001"));
    }

    [Fact]
    public async Task ObtenerTodosLosRecordatorios_Y_ObtenerPorId_CubreLecturaYProveedorDesconocido()
    {
        var fila = new Dictionary<string, object>
        {
            ["id_recordatorio"] = 5,
            ["id_usuario"] = 10,
            ["id_proveedor"] = "99",
            ["fecha_reg_recordatorio"] = DateTime.Today,
            ["fecha_entrega"] = DateTime.Today.AddDays(2),
            ["estado_recordatorio"] = 1
        };

        var (repo, context, session) = CrearRepositorio(rowsAffected: 1, rows: new() { fila });
        using var _ = context;
        context.Usuarios.Add(new Usuario { IdUsuario = 10, IdSede = 2, EstadoUsuario = true });
        await context.SaveChangesAsync();
        session.SetStringValue("SedeId", "2");

        var todos = await repo.ObtenerTodosLosRecordatorios();
        var porId = await repo.ObtenerPorId(5);

        Assert.Single(todos);
        Assert.Contains("99 - Desconocido", todos[0].Proveedor);
        Assert.NotNull(porId);
    }

    [Fact]
    public async Task ObtenerTodosLosRecordatorios_ViaUsuarioIdEnSesion_CubreRamaUsuario()
    {
        var (repo, context, session) = CrearRepositorio(rowsAffected: 1, rows: new());
        using var _ = context;
        context.Usuarios.Add(new Usuario { IdUsuario = 20, IdRol = 2, IdSede = 4, EstadoUsuario = true });
        await context.SaveChangesAsync();
        session.SetStringValue("UsuarioId", "20");

        var lista = await repo.ObtenerTodosLosRecordatorios();
        Assert.Empty(lista);
    }

    [Fact]
    public async Task CrearActualizarYActualizarEstadoRecordatorio_CubreRamasExitoYFallo()
    {
        var (repoOk, context, _) = CrearRepositorio(rowsAffected: 1);
        using var _ = context;

        var dtoOk = new RecordatorioPedidoDto
        {
            IdRecordatorio = "1",
            Proveedor = "1 - Colombina",
            FechaEntrega = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            Estado = "1"
        };

        Assert.True(await repoOk.CrearRecordatorio(dtoOk, 1));
        Assert.True(await repoOk.ActualizarRecordatorio(dtoOk));
        Assert.True(await repoOk.ActualizarEstadoRecordatorio(1, 1));

        var (repoFail, _, _) = CrearRepositorio(rowsAffected: 0);
        Assert.False(await repoFail.CrearRecordatorio(dtoOk, 1));
        Assert.False(await repoFail.ActualizarRecordatorio(dtoOk));
        Assert.False(await repoFail.ActualizarEstadoRecordatorio(1, 0));
    }

    [Fact]
    public async Task CrearYActualizarRecordatorio_ProveedorInvalido_RetornaFalse()
    {
        var (repo, context, _) = CrearRepositorio();
        using var _ = context;
        var dto = new RecordatorioPedidoDto { IdRecordatorio = "1", Proveedor = "0", FechaEntrega = DateOnly.FromDateTime(DateTime.Today), Estado = "0" };

        Assert.False(await repo.CrearRecordatorio(dto, 1));
        Assert.False(await repo.ActualizarRecordatorio(dto));
    }

    [Fact]
    public async Task SinHttpContext_O_SinUsuarios_RetornaVacioONull()
    {
        var (repoSinContext, context, _) = CrearRepositorio(conHttpContext: false);
        using var _ = context;

        Assert.Empty(await repoSinContext.ObtenerTodosLosRecordatorios());
        Assert.Null(await repoSinContext.ObtenerPorId(1));
    }
}
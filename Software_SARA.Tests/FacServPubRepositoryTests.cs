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
using Moq;
using Software_SARA.Dtos;
using Software_SARA.Repositories;
using Software_SARA.Repositories.Models;
using Software_SARA.Utilities;
using Xunit;

public class FacServPubRepositoryTests
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

    private sealed class FakeDBSaraUtility : IDBSaraUtility
    {
        private readonly DbConnection _conn;
        public FakeDBSaraUtility(int rowsAffected, List<Dictionary<string, object>>? rows = null)
            => _conn = new FakeDbConnection(rowsAffected, rows);
        public void Connect() { }
        public void Disconnect() { }
        public DbConnection GetDbConnection() => _conn;
    }

    private static (FacServPubRepository repo, SarabdContext context, TestSession session)
        CrearRepositorio(int rowsAffected = 1, List<Dictionary<string, object>>? rows = null, bool conHttpContext = true)
    {
        var options = new DbContextOptionsBuilder<SarabdContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString(), b => b.EnableNullChecks(false))
            .Options;

        var context = new SarabdContext(options);
        var session = new TestSession();
        var accessor = new Mock<IHttpContextAccessor>();

        if (conHttpContext)
        {
            accessor.Setup(a => a.HttpContext).Returns(new DefaultHttpContext { Session = session });
        }
        else
        {
            accessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);
        }

        var repo = new FacServPubRepository(
            context,
            accessor.Object,
            () => new FakeDBSaraUtility(rowsAffected, rows));

        return (repo, context, session);
    }

    [Fact]
    public async Task ObtenerTodasLasFacturasServiciosPublicos_CubreLecturaYValoresNulos()
    {
        var filaCompleta = new Dictionary<string, object>
        {
            ["id_factura"] = 1,
            ["id_usuario"] = 10,
            ["tip_factura"] = true,
            ["id_proveedor"] = 2,
            ["nom_servicio"] = "Energía",
            ["id_servicio"] = 3,
            ["valor_factura"] = 45000m,
            ["fecha_reg_factura"] = DateTime.Today,
            ["fecha_vencimiento"] = DateTime.Today.AddDays(5),
            ["estado_factura"] = true
        };

        var filaNula = new Dictionary<string, object>
        {
            ["id_factura"] = DBNull.Value,
            ["id_usuario"] = DBNull.Value,
            ["tip_factura"] = DBNull.Value,
            ["id_proveedor"] = DBNull.Value,
            ["nom_servicio"] = DBNull.Value,
            ["id_servicio"] = DBNull.Value,
            ["valor_factura"] = DBNull.Value,
            ["fecha_reg_factura"] = DBNull.Value,
            ["fecha_vencimiento"] = DBNull.Value,
            ["estado_factura"] = DBNull.Value
        };

        var (repo, context, session) = CrearRepositorio(rows: new() { filaCompleta, filaNula });
        using var _ = context;
        context.Usuarios.Add(new Usuario { IdUsuario = 10, IdSede = 1, EstadoUsuario = true });
        await context.SaveChangesAsync();
        session.SetStringValue("SedeId", "1");

        var resultado = await repo.ObtenerTodasLasFacturasServiciosPublicos();

        Assert.Equal(2, resultado.Count);
        Assert.Equal("Sin servicio público", resultado[1].NomProveedor);
    }

    [Fact]
    public async Task ObtenerHistorialFacturasServiciosPublicosAsync_CubreLecturaYValoresNulos()
    {
        var fila = new Dictionary<string, object>
        {
            ["id_factura"] = 9,
            ["id_usuario"] = 10,
            ["nom_usuario"] = DBNull.Value,
            ["tip_factura"] = false,
            ["id_proveedor"] = DBNull.Value,
            ["id_servicio"] = 1,
            ["nom_servicio"] = DBNull.Value,
            ["valor_factura"] = 12000m,
            ["fecha_reg_factura"] = DateTime.Today,
            ["fecha_vencimiento"] = DateTime.Today.AddDays(2),
            ["fecha_pago"] = DBNull.Value,
            ["estado_factura"] = false
        };

        var (repo, context, session) = CrearRepositorio(rows: new() { fila });
        using var _ = context;
        context.Usuarios.Add(new Usuario { IdUsuario = 10, IdRol = 1, IdSede = null, EstadoUsuario = true });
        await context.SaveChangesAsync();
        session.SetStringValue("UsuarioId", "10");

        var historial = await repo.ObtenerHistorialFacturasServiciosPublicosAsync();

        Assert.Single(historial);
        Assert.Equal("N/D", historial[0].NomUsuario);
        Assert.Equal("Sin servicio público", historial[0].NomServicio);
        Assert.Null(historial[0].FechaPago);
    }

    [Fact]
    public async Task ObtenerListaServiciosPublicos_RetornaItems()
    {
        var rows = new List<Dictionary<string, object>>
        {
            new() { ["id_servicio"] = 1, ["nom_servicio"] = "Agua" }
        };
        var (repo, context, _) = CrearRepositorio(rows: rows);
        using var _ = context;

        var lista = await repo.ObtenerListaServiciosPublicos();

        Assert.Single(lista);
        Assert.Equal("1", lista[0].Value);
        Assert.Equal("Agua", lista[0].Text);
    }

    [Theory]
    [InlineData(1, true, true)]
    [InlineData(0, false, false)]
    public async Task CreateYEditarYConfirmarPago_CubreExitoYFallo(int rowsAffected, bool flag, bool esperado)
    {
        var (repo, context, _) = CrearRepositorio(rowsAffected: rowsAffected);
        using var _ = context;

        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var dto = new FacServPublicDto
        {
            IdFactura = 1,
            IdUsuario = 5,
            TipFactura = flag,
            IdProveedor = flag ? 1 : null,
            IdServicio = flag ? 2 : null,
            ValorFactura = 20000m,
            FechaRegFactura = hoy,
            FechaVencimiento = hoy.AddDays(3),
            EstadoFactura = flag
        };

        Assert.Equal(esperado, await repo.CreateFacturaServicioPublico(dto));
        Assert.Equal(esperado, await repo.EditarFacturaServicioPublico(dto));
        Assert.Equal(esperado, await repo.ConfirmarPagoFacturaServicioPublico(1));
    }

    [Fact]
    public async Task SinHttpContext_O_SinUsuarios_RetornaVacio()
    {
        var (repoSinContext, context, _) = CrearRepositorio(conHttpContext: false);
        using var _ = context;

        Assert.Empty(await repoSinContext.ObtenerTodasLasFacturasServiciosPublicos());
        Assert.Empty(await repoSinContext.ObtenerHistorialFacturasServiciosPublicosAsync());
    }
}
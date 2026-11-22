using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Software_SARA.Dtos;
using Software_SARA.Repositories;
using Software_SARA.Repositories.Models;
using Software_SARA.Utilities;
using Xunit;

public class SedeRepositoryTests
{
    private sealed class FakeDbConnection : DbConnection
    {
        private readonly int _rowsAffected;
        public FakeDbConnection(int rowsAffected) => _rowsAffected = rowsAffected;
        [AllowNull] public override string ConnectionString { get; set; } = string.Empty;
        public override string Database => "SARABD";
        public override string DataSource => "Fake";
        public override string ServerVersion => "1.0";
        public override ConnectionState State => ConnectionState.Open;
        public override void ChangeDatabase(string databaseName) { }
        public override void Close() { }
        public override void Open() { }
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotImplementedException();
        protected override DbCommand CreateDbCommand() => new FakeDbCommand(_rowsAffected);
    }

    private sealed class FakeDbCommand : DbCommand
    {
        private readonly int _rowsAffected;
        private readonly FakeParameterCollection _params = new();
        public FakeDbCommand(int rowsAffected) => _rowsAffected = rowsAffected;
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
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotImplementedException();
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
        private readonly System.Collections.Generic.List<DbParameter> _items = new();
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
        public FakeDBSaraUtility(int rowsAffected) => _conn = new FakeDbConnection(rowsAffected);
        public void Connect() { }
        public void Disconnect() { }
        public DbConnection GetDbConnection() => _conn;
    }

    private static SarabdContext CrearContextoEnMemoria()
    {
        var options = new DbContextOptionsBuilder<SarabdContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString(), b => b.EnableNullChecks(false))
            .Options;

        return new SarabdContext(options);
    }

    [Fact]
    public async Task ObtenerTodasLasSedes_RetornaSedesRegistradas()
    {
        using var context = CrearContextoEnMemoria();
        context.Sedes.AddRange(
            new Sede { IdSede = 1, NomSede = "Sede Centro", EstadoSede = true },
            new Sede { IdSede = 2, NomSede = "Sede Norte", EstadoSede = false }
        );
        await context.SaveChangesAsync();

        var repository = new SedeRepository(context);
        var resultado = await repository.ObtenerTodasLasSedes();

        Assert.Equal(2, resultado.Count);
    }

    [Fact]
    public async Task TieneInformacionAsociadaAsync_CubreTodasLasRamas()
    {
        using var context = CrearContextoEnMemoria();
        context.Usuarios.AddRange(
            new Usuario { IdUsuario = 10, IdSede = 1 },
            new Usuario { IdUsuario = 11, IdSede = 2 },
            new Usuario { IdUsuario = 12, IdSede = 3 },
            new Usuario { IdUsuario = 13, IdSede = 4 },
            new Usuario { IdUsuario = 14, IdSede = 5 }
        );
        context.Facturas.Add(new Factura { IdUsuario = 11 });
        context.Recordatorios.Add(new Recordatorio { IdUsuario = 12 });
        context.DiaCajas.Add(new DiaCaja { IdUsuario = 13, IdUsuarioCierre = null });
        context.DiaCajas.Add(new DiaCaja { IdUsuario = 999, IdUsuarioCierre = 14 });
        await context.SaveChangesAsync();

        var repository = new SedeRepository(context);

        Assert.False(await repository.TieneInformacionAsociadaAsync(99)); // Sin usuarios
        Assert.False(await repository.TieneInformacionAsociadaAsync(1));  // Con usuario, sin registros
        Assert.True(await repository.TieneInformacionAsociadaAsync(2));   // Con facturas
        Assert.True(await repository.TieneInformacionAsociadaAsync(3));   // Con recordatorios
        Assert.True(await repository.TieneInformacionAsociadaAsync(4));   // Con caja abierta
        Assert.True(await repository.TieneInformacionAsociadaAsync(5));   // Con caja cerrada
    }

    [Theory]
    [InlineData(1, true, true)]
    [InlineData(0, false, false)]
    public async Task CreateSede_CubreExitoFalloYNulos(int filasAfectadas, bool estado, bool esperado)
    {
        using var context = CrearContextoEnMemoria();
        var repo = new SedeRepository(context, () => new FakeDBSaraUtility(filasAfectadas));

        var dto = estado
            ? new SedeDto { NomSede = "A", DirSede = "B", TelSede = "123", EstadoSede = true, CiuSede = "C", DepSede = "D", BarrioSede = "E" }
            : new SedeDto { EstadoSede = false }; // Prueba ramas ?? string.Empty

        Assert.Equal(esperado, await repo.CreateSede(dto));
    }

    [Theory]
    [InlineData(1, true, true)]
    [InlineData(0, false, false)]
    public async Task EditarSede_CubreExitoFalloYNulos(int filasAfectadas, bool estado, bool esperado)
    {
        using var context = CrearContextoEnMemoria();
        var repo = new SedeRepository(context, () => new FakeDBSaraUtility(filasAfectadas));

        var dto = estado
            ? new SedeDto { IdSede = 1, NomSede = "A", DirSede = "B", TelSede = "123", EstadoSede = true, CiuSede = "C", DepSede = "D", BarrioSede = "E" }
            : new SedeDto { IdSede = 1, EstadoSede = false };

        Assert.Equal(esperado, await repo.EditarSede(dto));
    }

    [Fact]
    public async Task EliminarSede_CubreConInformacionSinInformacionYFalloSql()
    {
        using var context = CrearContextoEnMemoria();
        context.Usuarios.Add(new Usuario { IdUsuario = 20, IdSede = 5 });
        context.Facturas.Add(new Factura { IdUsuario = 20 });
        await context.SaveChangesAsync();

        var repoExito = new SedeRepository(context, () => new FakeDBSaraUtility(1));
        var repoFallo = new SedeRepository(context, () => new FakeDBSaraUtility(0));

        Assert.False(await repoExito.EliminarSede(5));  // Tiene información asociada
        Assert.True(await repoExito.EliminarSede(99));  // Sin información, SQL afecta 1 fila
        Assert.False(await repoFallo.EliminarSede(99)); // Sin información, SQL afecta 0 filas
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    public async Task ReactivarSede_CubreExitoYFallo(int filasAfectadas, bool esperado)
    {
        using var context = CrearContextoEnMemoria();
        var repo = new SedeRepository(context, () => new FakeDBSaraUtility(filasAfectadas));

        Assert.Equal(esperado, await repo.ReactivarSede(1));
    }
}
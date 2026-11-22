using System.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Software_SARA.Dtos;
using Software_SARA.Repositories.Models;

namespace Software_SARA.Repositories
{
    public class RecordatorioPedidoRepository
    {
        private readonly string _connectionString;
        private readonly SarabdContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RecordatorioPedidoRepository(
            IConfiguration configuration,
            SarabdContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public static bool TryParseIdProveedor(string? valor, out int idProveedor)
        {
            idProveedor = 0;
            if (string.IsNullOrWhiteSpace(valor)) return false;

            string parteNumerica = valor.Split('-', StringSplitOptions.TrimEntries)[0];
            return int.TryParse(parteNumerica, out idProveedor) && idProveedor > 0;
        }

        private List<int> ObtenerIdsUsuariosSedeActual()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return new List<int>();

            int? sedeId = null;
            var sedeIdStr = httpContext.Session.GetString("SedeId");
            if (int.TryParse(sedeIdStr, out int parsedSedeId) && parsedSedeId > 0)
            {
                sedeId = parsedSedeId;
            }
            else
            {
                var usuarioIdStr = httpContext.Session.GetString("UsuarioId");
                if (int.TryParse(usuarioIdStr, out int usuarioId) && usuarioId > 0)
                {
                    var usuario = _context.Usuarios
                        .Where(u => u.IdUsuario == usuarioId)
                        .Select(u => new { u.IdRol, u.IdSede })
                        .FirstOrDefault();

                    if (usuario != null)
                    {
                        if (!usuario.IdSede.HasValue && (usuario.IdRol == 1 || usuario.IdRol == 5))
                        {
                            return _context.Usuarios
                                .Where(u => u.EstadoUsuario)
                                .Select(u => u.IdUsuario)
                                .ToList();
                        }

                        sedeId = usuario.IdSede;
                    }
                }
            }

            if (!sedeId.HasValue || sedeId.Value <= 0) return new List<int>();

            return _context.Usuarios
                .Where(u => u.IdSede == sedeId.Value)
                .Select(u => u.IdUsuario)
                .ToList();
        }

        private string ObtenerNombreProveedor(string idProveedorStr)
        {
            var proveedores = new Dictionary<string, string>
            {
                { "1", "Colombina" }, { "2", "Coca Cola" }, { "3", "Postobón" }, { "4", "Nutresa" },
                { "5", "Surtilider" }, { "6", "Nexpress" }, { "7", "Harinera del Valle" }, { "8", "Bimbo" },
                { "9", "Ramo" }, { "10", "Sulicor" }, { "11", "Hielo polar" }, { "12", "Casa Luker" },
                { "13", "Diana" }, { "14", "Alpina" }, { "15", "Organización Cárdenas" }, { "16", "Alquería" },
                { "17", "Altipal" }, { "18", "Colanta" }, { "19", "Super Ricas" }, { "20", "Margarita" },
                { "21", "Yupi" }, { "22", "Big Cola" }, { "23", "Tecnoquímica" }, { "24", "Corbeta" }
            };

            return proveedores.TryGetValue(idProveedorStr, out var nombre) ? nombre : "Desconocido";
        }

        public virtual async Task<List<RecordatorioPedidoDto>> ObtenerTodosLosRecordatorios()
        {
            var lista = new List<RecordatorioPedidoDto>();
            var idsUsuariosSede = ObtenerIdsUsuariosSedeActual();
            if (idsUsuariosSede.Count == 0) return lista;

            var parametros = idsUsuariosSede.Select((id, idx) => new SqlParameter($"@u{idx}", id)).ToArray();
            string inClause = string.Join(", ", parametros.Select(p => p.ParameterName));

            using var conexion = new SqlConnection(_connectionString);
            await conexion.OpenAsync();

            string query = $@"SELECT id_recordatorio, id_usuario, id_proveedor, fecha_reg_recordatorio, fecha_entrega, estado_recordatorio
                             FROM dbo.recordatorio
                             WHERE estado_recordatorio = 0 AND id_usuario IN ({inClause})";

            using var comando = new SqlCommand(query, conexion);
            comando.Parameters.AddRange(parametros);

            using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
            {
                int estadoDb = lector["estado_recordatorio"] != DBNull.Value ? Convert.ToInt32(lector["estado_recordatorio"]) : 0;
                string estadoTexto = estadoDb == 1 ? "Entregado" : "Pendiente";
                string idDb = lector["id_recordatorio"]?.ToString() ?? "1";
                string idProv = lector["id_proveedor"]?.ToString() ?? "";
                string nombreProv = ObtenerNombreProveedor(idProv);

                lista.Add(new RecordatorioPedidoDto
                {
                    IdRecordatorio = idDb.PadLeft(3, '0'),
                    Proveedor = $"{idProv} - {nombreProv}",
                    FechaEntrega = lector["fecha_entrega"] != DBNull.Value
                        ? DateOnly.FromDateTime(Convert.ToDateTime(lector["fecha_entrega"]))
                        : DateOnly.FromDateTime(DateTime.Now),
                    Estado = estadoTexto
                });
            }

            return lista;
        }

        public virtual async Task<bool> ExisteIdRecordatorio(string idRecordatorio)
        {
            return await Task.FromResult(false);
        }

        public virtual async Task<bool> CrearRecordatorio(RecordatorioPedidoDto dto, int idUsuarioLogueado)
        {
            if (!TryParseIdProveedor(dto.Proveedor, out int idProveedor))
            {
                return false;
            }

            using var conexion = new SqlConnection(_connectionString);
            await conexion.OpenAsync();

            const string query = @"INSERT INTO dbo.recordatorio (id_usuario, id_proveedor, fecha_reg_recordatorio, fecha_entrega, estado_recordatorio) 
                                   VALUES (@IdUsuario, @IdProveedor, @FechaReg, @FechaEntrega, @Estado)";

            using var comando = new SqlCommand(query, conexion);
            int.TryParse(dto.Estado, out int estadoValor);

            comando.Parameters.AddWithValue("@IdUsuario", idUsuarioLogueado);
            comando.Parameters.AddWithValue("@IdProveedor", idProveedor);
            comando.Parameters.AddWithValue("@FechaReg", DateTime.Now);
            comando.Parameters.AddWithValue("@FechaEntrega", dto.FechaEntrega.ToDateTime(TimeOnly.MinValue));
            comando.Parameters.AddWithValue("@Estado", estadoValor);

            int filasAfectadas = await comando.ExecuteNonQueryAsync();
            return filasAfectadas > 0;
        }

        public virtual async Task<RecordatorioPedidoDto?> ObtenerPorId(int idRecordatorio)
        {
            var idsUsuariosSede = ObtenerIdsUsuariosSedeActual();
            if (idsUsuariosSede.Count == 0) return null;

            var parametros = idsUsuariosSede.Select((id, idx) => new SqlParameter($"@u{idx}", id)).ToArray();
            string inClause = string.Join(", ", parametros.Select(p => p.ParameterName));

            using var conexion = new SqlConnection(_connectionString);
            await conexion.OpenAsync();

            string query = $@"SELECT id_recordatorio, id_usuario, id_proveedor, fecha_reg_recordatorio, fecha_entrega, estado_recordatorio
                             FROM dbo.recordatorio
                             WHERE id_recordatorio = @Id AND id_usuario IN ({inClause})";

            using var comando = new SqlCommand(query, conexion);
            comando.Parameters.AddWithValue("@Id", idRecordatorio);
            comando.Parameters.AddRange(parametros);

            using var lector = await comando.ExecuteReaderAsync();
            if (await lector.ReadAsync())
            {
                int estadoDb = lector["estado_recordatorio"] != DBNull.Value ? Convert.ToInt32(lector["estado_recordatorio"]) : 0;
                string estadoTexto = estadoDb == 1 ? "Entregado" : "Pendiente";
                string idDb = lector["id_recordatorio"]?.ToString() ?? "1";
                string idProv = lector["id_proveedor"]?.ToString() ?? "";
                string nombreProv = ObtenerNombreProveedor(idProv);

                return new RecordatorioPedidoDto
                {
                    IdRecordatorio = idDb.PadLeft(3, '0'),
                    Proveedor = $"{idProv} - {nombreProv}",
                    FechaEntrega = lector["fecha_entrega"] != DBNull.Value
                        ? DateOnly.FromDateTime(Convert.ToDateTime(lector["fecha_entrega"]))
                        : DateOnly.FromDateTime(DateTime.Now),
                    Estado = estadoTexto
                };
            }

            return null;
        }

        public virtual async Task<bool> ActualizarRecordatorio(RecordatorioPedidoDto dto)
        {
            if (!TryParseIdProveedor(dto.Proveedor, out int idProveedor))
            {
                return false;
            }

            using var conexion = new SqlConnection(_connectionString);
            await conexion.OpenAsync();
            int idRecordatorioNum = Convert.ToInt32(dto.IdRecordatorio);

            const string query = @"UPDATE dbo.recordatorio 
                                   SET id_proveedor = @IdProveedor, 
                                       fecha_entrega = @FechaEntrega, 
                                       estado_recordatorio = @Estado 
                                   WHERE id_recordatorio = @IdRecordatorio";

            using var comando = new SqlCommand(query, conexion);
            int.TryParse(dto.Estado, out int estadoValor);

            comando.Parameters.AddWithValue("@IdProveedor", idProveedor);
            comando.Parameters.AddWithValue("@FechaEntrega", dto.FechaEntrega.ToDateTime(TimeOnly.MinValue));
            comando.Parameters.AddWithValue("@Estado", estadoValor);
            comando.Parameters.AddWithValue("@IdRecordatorio", idRecordatorioNum);

            int filasAfectadas = await comando.ExecuteNonQueryAsync();
            return filasAfectadas > 0;
        }

        public virtual async Task<bool> ActualizarEstadoRecordatorio(int idRecordatorio, int nuevoEstado)
        {
            using var conexion = new SqlConnection(_connectionString);
            await conexion.OpenAsync();
            const string query = "UPDATE dbo.recordatorio SET estado_recordatorio = @Estado WHERE id_recordatorio = @Id";

            using var comando = new SqlCommand(query, conexion);
            comando.Parameters.AddWithValue("@Estado", nuevoEstado);
            comando.Parameters.AddWithValue("@Id", idRecordatorio);

            int filas = await comando.ExecuteNonQueryAsync();
            return filas > 0;
        }
    }
}
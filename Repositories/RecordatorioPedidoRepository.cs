using System.Data;
using Microsoft.Data.SqlClient;
using Software_SARA.Dtos;

namespace Software_SARA.Repositories
{
    public class RecordatorioPedidoRepository
    {
        private readonly string _connectionString;

        public RecordatorioPedidoRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        // Método auxiliar para traducir el ID del proveedor a su Nombre respectivo
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

            return proveedores.ContainsKey(idProveedorStr) ? proveedores[idProveedorStr] : "Desconocido";
        }

        public async Task<List<RecordatorioPedidoDto>> ObtenerTodosLosRecordatorios()
        {
            var lista = new List<RecordatorioPedidoDto>();

            using (var conexion = new SqlConnection(_connectionString))
            {
                await conexion.OpenAsync();
                // Filtramos para que solo cargue los pendientes en la vista principal
                string query = "SELECT id_recordatorio, id_usuario, id_proveedor, fecha_reg_recordatorio, fecha_entrega, estado_recordatorio FROM dbo.recordatorio WHERE estado_recordatorio = 0";

                using (var comando = new SqlCommand(query, conexion))
                {
                    using (var lector = await comando.ExecuteReaderAsync())
                    {
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
                                Proveedor = $"{idProv} - {nombreProv}", // Muestra Ej: "1 - Colombina" en una línea limpia
                                FechaEntrega = lector["fecha_entrega"] != DBNull.Value ? DateOnly.FromDateTime(Convert.ToDateTime(lector["fecha_entrega"])) : DateOnly.FromDateTime(DateTime.Now),
                                Estado = estadoTexto
                            });
                        }
                    }
                }
            }

            return lista;
        }

        public async Task<bool> ExisteIdRecordatorio(string idRecordatorio)
        {
            return await Task.FromResult(false);
        }

        public async Task<bool> CrearRecordatorio(RecordatorioPedidoDto dto, int idUsuarioLogueado)
        {
            using (var conexion = new SqlConnection(_connectionString))
            {
                await conexion.OpenAsync();

                string query = @"INSERT INTO dbo.recordatorio (id_usuario, id_proveedor, fecha_reg_recordatorio, fecha_entrega, estado_recordatorio) 
                                 VALUES (@IdUsuario, @IdProveedor, @FechaReg, @FechaEntrega, @Estado)";

                using (var comando = new SqlCommand(query, conexion))
                {
                    int estadoValor = 0;
                    int.TryParse(dto.Estado, out estadoValor);

                    comando.Parameters.AddWithValue("@IdUsuario", idUsuarioLogueado);
                    comando.Parameters.AddWithValue("@IdProveedor", Convert.ToInt32(dto.Proveedor));
                    comando.Parameters.AddWithValue("@FechaReg", DateTime.Now);
                    comando.Parameters.AddWithValue("@FechaEntrega", dto.FechaEntrega.ToDateTime(TimeOnly.MinValue));
                    comando.Parameters.AddWithValue("@Estado", estadoValor);

                    int filasAfectadas = await comando.ExecuteNonQueryAsync();
                    return filasAfectadas > 0;
                }
            }
        }

        public async Task<RecordatorioPedidoDto> ObtenerPorId(int idRecordatorio)
        {
            using (var conexion = new SqlConnection(_connectionString))
            {
                await conexion.OpenAsync();
                string query = "SELECT id_recordatorio, id_usuario, id_proveedor, fecha_reg_recordatorio, fecha_entrega, estado_recordatorio FROM dbo.recordatorio WHERE id_recordatorio = @Id";

                using (var comando = new SqlCommand(query, conexion))
                {
                    comando.Parameters.AddWithValue("@Id", idRecordatorio);
                    using (var lector = await comando.ExecuteReaderAsync())
                    {
                        if (await lector.ReadAsync())
                        {
                            int estadoDb = lector["estado_recordatorio"] != DBNull.Value ? Convert.ToInt32(lector["estado_recordatorio"]) : 0;
                            string estadoTexto = estadoDb == 1 ? "Entregado" : "Pendiente"; // Traduce el estado correctamente a texto
                            string idDb = lector["id_recordatorio"]?.ToString() ?? "1";
                            string idProv = lector["id_proveedor"]?.ToString() ?? "";
                            string nombreProv = ObtenerNombreProveedor(idProv);

                            return new RecordatorioPedidoDto
                            {
                                IdRecordatorio = idDb.PadLeft(3, '0'),
                                Proveedor = $"{idProv} - {nombreProv}",
                                FechaEntrega = lector["fecha_entrega"] != DBNull.Value ? DateOnly.FromDateTime(Convert.ToDateTime(lector["fecha_entrega"])) : DateOnly.FromDateTime(DateTime.Now),
                                Estado = estadoTexto
                            };
                        }
                    }
                }
            }
            return null;
        }

        public async Task<bool> ActualizarRecordatorio(RecordatorioPedidoDto dto)
        {
            using (var conexion = new SqlConnection(_connectionString))
            {
                await conexion.OpenAsync();
                int idRecordatorioNum = Convert.ToInt32(dto.IdRecordatorio);

                string query = @"UPDATE dbo.recordatorio 
                                 SET id_proveedor = @IdProveedor, 
                                     fecha_entrega = @FechaEntrega, 
                                     estado_recordatorio = @Estado 
                                 WHERE id_recordatorio = @IdRecordatorio";

                using (var comando = new SqlCommand(query, conexion))
                {
                    int estadoValor = 0;
                    int.TryParse(dto.Estado, out estadoValor);

                    comando.Parameters.AddWithValue("@IdProveedor", Convert.ToInt32(dto.Proveedor));
                    comando.Parameters.AddWithValue("@FechaEntrega", dto.FechaEntrega.ToDateTime(TimeOnly.MinValue));
                    comando.Parameters.AddWithValue("@Estado", estadoValor);
                    comando.Parameters.AddWithValue("@IdRecordatorio", idRecordatorioNum);

                    int filasAfectadas = await comando.ExecuteNonQueryAsync();
                    return filasAfectadas > 0;
                }
            }
        }

        public async Task<bool> ActualizarEstadoRecordatorio(int idRecordatorio, int nuevoEstado)
        {
            using (var conexion = new SqlConnection(_connectionString))
            {
                await conexion.OpenAsync();
                string query = "UPDATE dbo.recordatorio SET estado_recordatorio = @Estado WHERE id_recordatorio = @Id";

                using (var comando = new SqlCommand(query, conexion))
                {
                    comando.Parameters.AddWithValue("@Estado", nuevoEstado);
                    comando.Parameters.AddWithValue("@Id", idRecordatorio);

                    int filas = await comando.ExecuteNonQueryAsync();
                    return filas > 0;
                }
            }
        }
    }
}
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Software_SARA.Dtos;
using Software_SARA.Repositories.Models;
using Software_SARA.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data; // IMPORTANTE: Necesario para usar CommandType.StoredProcedure

namespace Software_SARA.Repositories
{
    public class FacProveedorRepository
    {
        private readonly SarabdContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public FacProveedorRepository(SarabdContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        private List<int> ObtenerIdsUsuariosSedeActual()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return new List<int>();

            var sedeIdStr = httpContext.Session.GetString("SedeId");
            if (!int.TryParse(sedeIdStr, out int sedeId) || sedeId <= 0)
            {
                var usuarioIdStr = httpContext.Session.GetString("UsuarioId");
                if (int.TryParse(usuarioIdStr, out int usuarioId) && usuarioId > 0)
                {
                    sedeId = _context.Usuarios
                        .Where(u => u.IdUsuario == usuarioId)
                        .Select(u => u.IdSede)
                        .FirstOrDefault();
                }
            }

            if (sedeId <= 0) return new List<int>();

            return _context.Usuarios
                .Where(u => u.IdSede == sedeId)
                .Select(u => u.IdUsuario)
                .ToList();
        }

        public async Task<List<Factura>> ObtenerTodasLasFacturasProveedores()
        {
            var listaFacturas = new List<Factura>();
            var idsUsuariosSede = ObtenerIdsUsuariosSedeActual();
            if (idsUsuariosSede.Count == 0) return listaFacturas;

            var parametros = idsUsuariosSede.Select((id, idx) => new SqlParameter($"@u{idx}", id)).ToArray();
            string inClause = string.Join(", ", parametros.Select(p => p.ParameterName));

            string sql = $@"SELECT f.id_factura, f.id_usuario, f.tip_factura, f.id_proveedor,
                                   p.nom_proveedor, f.id_servicio, f.valor_factura,
                                   f.fecha_reg_factura, f.fecha_vencimiento, f.estado_factura
                            FROM SARABD.dbo.factura f
                            LEFT JOIN SARABD.dbo.proveedor p ON f.id_proveedor = p.id_proveedor
                            WHERE f.estado_factura = 1 AND f.id_proveedor IS NOT NULL
                              AND f.id_usuario IN ({inClause})
                            ORDER BY f.id_factura DESC;";

            DBSaraUtility connectionUtility = new DBSaraUtility();
            connectionUtility.Connect();

            try
            {
                using (SqlCommand command = new SqlCommand(sql, connectionUtility.CONN()))
                {
                    command.Parameters.AddRange(parametros);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var factura = new Factura
                            {
                                IdFactura = reader["id_factura"] != DBNull.Value ? Convert.ToInt32(reader["id_factura"]) : 0,
                                IdUsuario = reader["id_usuario"] != DBNull.Value ? Convert.ToInt32(reader["id_usuario"]) : 0,
                                TipFactura = reader["tip_factura"] != DBNull.Value && Convert.ToBoolean(reader["tip_factura"]),
                                IdProveedor = reader["id_proveedor"] != DBNull.Value ? Convert.ToInt32(reader["id_proveedor"]) : (int?)null,
                                NomProveedor = reader["nom_proveedor"] != DBNull.Value ? reader["nom_proveedor"].ToString() : "Sin Proveedor",
                                IdServicio = reader["id_servicio"] != DBNull.Value ? Convert.ToInt32(reader["id_servicio"]) : (int?)null,
                                ValorFactura = reader["valor_factura"] != DBNull.Value ? Convert.ToDecimal(reader["valor_factura"]) : 0m,
                                FechaRegFactura = reader["fecha_reg_factura"] != DBNull.Value ? DateOnly.FromDateTime(Convert.ToDateTime(reader["fecha_reg_factura"])) : default,
                                FechaVencimiento = reader["fecha_vencimiento"] != DBNull.Value ? DateOnly.FromDateTime(Convert.ToDateTime(reader["fecha_vencimiento"])) : default,
                                EstadoFactura = reader["estado_factura"] != DBNull.Value && Convert.ToBoolean(reader["estado_factura"])
                            };
                            listaFacturas.Add(factura);
                        }
                    }
                }
            }
            finally
            {
                connectionUtility.Disconnect();
            }

            return listaFacturas;
        }

        public async Task<bool> CreateFacturaProveedor(FacProveedorDto facProveedorDto)
        {
            bool result = false;
            string SQL = @"INSERT INTO SARABD.dbo.factura
                           (id_usuario, tip_factura, id_proveedor, id_servicio, valor_factura, fecha_reg_factura, fecha_vencimiento, estado_factura)
                           VALUES (@IdUsuario, @TipFactura, @IdProveedor, @IdServicio, @ValorFactura, @FechaRegFactura, @FechaVencimiento, @EstadoFactura);";

            DBSaraUtility connectionUtility = new DBSaraUtility();
            connectionUtility.Connect();

            try
            {
                using (SqlCommand command = new SqlCommand(SQL, connectionUtility.CONN()))
                {
                    command.Parameters.AddWithValue("@IdUsuario", facProveedorDto.IdUsuario);
                    command.Parameters.AddWithValue("@TipFactura", facProveedorDto.TipFactura ? 1 : 0);
                    command.Parameters.AddWithValue("@IdProveedor", facProveedorDto.IdProveedor.HasValue ? facProveedorDto.IdProveedor.Value : DBNull.Value);
                    command.Parameters.AddWithValue("@IdServicio", facProveedorDto.IdServicio.HasValue ? facProveedorDto.IdServicio.Value : DBNull.Value);
                    command.Parameters.AddWithValue("@ValorFactura", facProveedorDto.ValorFactura);
                    command.Parameters.AddWithValue("@FechaRegFactura", facProveedorDto.FechaRegFactura.ToString("yyyy-MM-dd"));
                    command.Parameters.AddWithValue("@FechaVencimiento", facProveedorDto.FechaVencimiento.ToString("yyyy-MM-dd"));
                    command.Parameters.AddWithValue("@EstadoFactura", facProveedorDto.EstadoFactura ? 1 : 0);

                    int comando = await command.ExecuteNonQueryAsync();
                    result = comando != 0;
                }
            }
            finally
            {
                connectionUtility.Disconnect();
            }

            return result;
        }

        public async Task<List<SelectListItem>> ObtenerListaProveedores()
        {
            var listaProveedores = new List<SelectListItem>();
            string sql = "SELECT id_proveedor, nom_proveedor FROM SARABD.dbo.proveedor ORDER BY nom_proveedor ASC;";

            DBSaraUtility connectionUtility = new DBSaraUtility();
            connectionUtility.Connect();

            try
            {
                using (SqlCommand command = new SqlCommand(sql, connectionUtility.CONN()))
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        listaProveedores.Add(new SelectListItem
                        {
                            Value = reader["id_proveedor"].ToString(),
                            Text = reader["nom_proveedor"].ToString()
                        });
                    }
                }
            }
            finally
            {
                connectionUtility.Disconnect();
            }

            return listaProveedores;
        }

        public async Task<bool> EditarFacturaProveedor(FacProveedorDto facProveedorDto)
        {
            bool result = false;
            string SQL = @"UPDATE SARABD.dbo.factura
                           SET id_usuario = @IdUsuario,
                               id_proveedor = @IdProveedor,
                               id_servicio = @IdServicio,
                               valor_factura = @ValorFactura,
                               fecha_reg_factura = @FechaRegFactura,
                               fecha_vencimiento = @FechaVencimiento,
                               estado_factura = @EstadoFactura
                           WHERE id_factura = @IdFactura;";

            DBSaraUtility connectionUtility = new DBSaraUtility();
            connectionUtility.Connect();

            try
            {
                using (SqlCommand command = new SqlCommand(SQL, connectionUtility.CONN()))
                {
                    command.Parameters.AddWithValue("@IdUsuario", facProveedorDto.IdUsuario);
                    command.Parameters.AddWithValue("@IdProveedor", facProveedorDto.IdProveedor.HasValue ? facProveedorDto.IdProveedor.Value : DBNull.Value);
                    command.Parameters.AddWithValue("@IdServicio", facProveedorDto.IdServicio.HasValue ? facProveedorDto.IdServicio.Value : DBNull.Value);
                    command.Parameters.AddWithValue("@ValorFactura", facProveedorDto.ValorFactura);
                    command.Parameters.AddWithValue("@FechaRegFactura", facProveedorDto.FechaRegFactura.ToString("yyyy-MM-dd"));
                    command.Parameters.AddWithValue("@FechaVencimiento", facProveedorDto.FechaVencimiento.ToString("yyyy-MM-dd"));
                    command.Parameters.AddWithValue("@EstadoFactura", facProveedorDto.EstadoFactura ? 1 : 0);
                    command.Parameters.AddWithValue("@IdFactura", facProveedorDto.IdFactura);

                    int comando = await command.ExecuteNonQueryAsync();
                    result = comando != 0;
                }
            }
            finally
            {
                connectionUtility.Disconnect();
            }

            return result;
        }

        public async Task<bool> ConfirmarPagoFacturaProveedor(int idFactura)
        {
            bool result = false;
            string fechaActual = DateTime.Now.ToString("yyyy-MM-dd");
            string SQL = "UPDATE SARABD.dbo.factura SET estado_factura = 0, fecha_pago = @FechaPago WHERE id_factura = @IdFactura;";

            DBSaraUtility connectionUtility = new DBSaraUtility();
            connectionUtility.Connect();

            try
            {
                using (SqlCommand command = new SqlCommand(SQL, connectionUtility.CONN()))
                {
                    command.Parameters.AddWithValue("@FechaPago", fechaActual);
                    command.Parameters.AddWithValue("@IdFactura", idFactura);

                    int comando = await command.ExecuteNonQueryAsync();
                    result = comando != 0;
                }
            }
            finally
            {
                connectionUtility.Disconnect();
            }

            return result;
        }

        public async Task<List<HistorialFacturaDto>> ObtenerHistorialFacturasProveedoresAsync()
        {
            var lista = new List<FacProveedorDto>();
            var idsUsuariosSede = ObtenerIdsUsuariosSedeActual();
            if (idsUsuariosSede.Count == 0) return lista;

            var parametros = idsUsuariosSede.Select((id, idx) => new SqlParameter($"@u{idx}", id)).ToArray();
            string inClause = string.Join(", ", parametros.Select(p => p.ParameterName));

            string sql = $@"SELECT f.id_factura, f.id_usuario, u.nom_usuario AS nom_usuario,
                                   f.tip_factura, f.id_proveedor, p.nom_proveedor,
                                   f.id_servicio, f.valor_factura, f.fecha_reg_factura,
                                   f.fecha_vencimiento, f.fecha_pago, f.estado_factura
                            FROM SARABD.dbo.factura f
                            LEFT JOIN SARABD.dbo.usuario u ON f.id_usuario = u.id_usuario
                            LEFT JOIN SARABD.dbo.proveedor p ON f.id_proveedor = p.id_proveedor
                            WHERE f.estado_factura = 0 AND f.id_proveedor IS NOT NULL
                              AND f.id_usuario IN ({inClause})
                            ORDER BY f.id_factura DESC;";

            DBSaraUtility connectionUtility = new DBSaraUtility();
            connectionUtility.Connect();

            try
            {
                using (SqlCommand command = new SqlCommand(sql, connectionUtility.CONN()))
                {
                    command.Parameters.AddRange(parametros);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            lista.Add(new FacProveedorDto
                            {
                                IdFactura = reader["id_factura"] != DBNull.Value ? Convert.ToInt32(reader["id_factura"]) : 0,
                                IdUsuario = reader["id_usuario"] != DBNull.Value ? Convert.ToInt32(reader["id_usuario"]) : 0,
                                NomUsuario = reader["nom_usuario"] != DBNull.Value ? reader["nom_usuario"].ToString() : "N/D",
                                TipFactura = reader["tip_factura"] != DBNull.Value && Convert.ToBoolean(reader["tip_factura"]),
                                IdProveedor = reader["id_proveedor"] != DBNull.Value ? Convert.ToInt32(reader["id_proveedor"]) : (int?)null,
                                NomProveedor = reader["nom_proveedor"] != DBNull.Value ? reader["nom_proveedor"].ToString() : "Sin Proveedor",
                                IdServicio = reader["id_servicio"] != DBNull.Value ? Convert.ToInt32(reader["id_servicio"]) : (int?)null,
                                ValorFactura = reader["valor_factura"] != DBNull.Value ? Convert.ToDecimal(reader["valor_factura"]) : 0m,
                                FechaRegFactura = reader["fecha_reg_factura"] != DBNull.Value ? DateOnly.FromDateTime(Convert.ToDateTime(reader["fecha_reg_factura"])) : default,
                                FechaVencimiento = reader["fecha_vencimiento"] != DBNull.Value ? DateOnly.FromDateTime(Convert.ToDateTime(reader["fecha_vencimiento"])) : default,
                                FechaPago = reader["fecha_pago"] != DBNull.Value ? Convert.ToDateTime(reader["fecha_pago"]) : (DateTime?)null,
                                EstadoFactura = reader["estado_factura"] != DBNull.Value && Convert.ToBoolean(reader["estado_factura"])
                            });
                        }
                    }
                }
            }
            finally
            {
                connectionUtility.Disconnect();
            }

            return lista;
        }
    }
}
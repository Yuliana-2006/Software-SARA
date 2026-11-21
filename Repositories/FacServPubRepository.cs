using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Software_SARA.Dtos;
using Software_SARA.Repositories.Models;
using Software_SARA.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Software_SARA.Repositories
{
    public class FacServPubRepository
    {
        private readonly SarabdContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public FacServPubRepository(SarabdContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
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

        public async Task<List<Factura>> ObtenerTodasLasFacturasServiciosPublicos()
        {
            var listaFacturas = new List<Factura>();
            var idsUsuariosSede = ObtenerIdsUsuariosSedeActual();
            if (idsUsuariosSede.Count == 0) return listaFacturas;

            var parametros = idsUsuariosSede.Select((id, idx) => new SqlParameter($"@u{idx}", id)).ToArray();
            string inClause = string.Join(", ", parametros.Select(p => p.ParameterName));

            string sql = $@"SELECT f.id_factura, f.id_usuario, f.tip_factura, f.id_proveedor,
                                   s.nom_servicio, f.id_servicio, f.valor_factura,
                                   f.fecha_reg_factura, f.fecha_vencimiento, f.estado_factura
                            FROM SARABD.dbo.factura f
                            LEFT JOIN SARABD.dbo.servicio s ON f.id_servicio = s.id_servicio
                            WHERE f.estado_factura = 1 AND f.id_servicio IS NOT NULL
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
                                NomProveedor = reader["nom_servicio"] != DBNull.Value ? reader["nom_servicio"].ToString() : "Sin servicio público",
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

        public async Task<bool> CreateFacturaServicioPublico(FacServPublicDto facServPublicDto)
        {
            // Consulta SQL para insertar una nueva factura de servicio público en la base de datos
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
                    command.Parameters.AddWithValue("@IdUsuario", facServPublicDto.IdUsuario);
                    command.Parameters.AddWithValue("@TipFactura", facServPublicDto.TipFactura ? 1 : 0);
                    command.Parameters.AddWithValue("@IdProveedor", facServPublicDto.IdProveedor.HasValue ? facServPublicDto.IdProveedor.Value : DBNull.Value);
                    command.Parameters.AddWithValue("@IdServicio", facServPublicDto.IdServicio.HasValue ? facServPublicDto.IdServicio.Value : DBNull.Value);
                    command.Parameters.AddWithValue("@ValorFactura", facServPublicDto.ValorFactura);
                    command.Parameters.AddWithValue("@FechaRegFactura", facServPublicDto.FechaRegFactura.ToString("yyyy-MM-dd"));
                    command.Parameters.AddWithValue("@FechaVencimiento", facServPublicDto.FechaVencimiento.ToString("yyyy-MM-dd"));
                    command.Parameters.AddWithValue("@EstadoFactura", facServPublicDto.EstadoFactura ? 1 : 0);

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

        public async Task<List<SelectListItem>> ObtenerListaServiciosPublicos()
        {
            var listaServiciosPublicos = new List<SelectListItem>();
            string sql = "SELECT id_servicio, nom_servicio FROM SARABD.dbo.servicio ORDER BY nom_servicio ASC;";

            DBSaraUtility connectionUtility = new DBSaraUtility();
            connectionUtility.Connect();

            try
            {
                using (SqlCommand command = new SqlCommand(sql, connectionUtility.CONN()))
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        listaServiciosPublicos.Add(new SelectListItem
                        {
                            Value = reader["id_servicio"].ToString(),
                            Text = reader["nom_servicio"].ToString()
                        });
                    }
                }
            }
            finally
            {
                connectionUtility.Disconnect();
            }

            return listaServiciosPublicos;
        }

        public async Task<bool> EditarFacturaServicioPublico(FacServPublicDto facServPublicDto)
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
                    command.Parameters.AddWithValue("@IdUsuario", facServPublicDto.IdUsuario);
                    command.Parameters.AddWithValue("@IdProveedor", facServPublicDto.IdProveedor.HasValue ? facServPublicDto.IdProveedor.Value : DBNull.Value);
                    command.Parameters.AddWithValue("@IdServicio", facServPublicDto.IdServicio.HasValue ? facServPublicDto.IdServicio.Value : DBNull.Value);
                    command.Parameters.AddWithValue("@ValorFactura", facServPublicDto.ValorFactura);
                    command.Parameters.AddWithValue("@FechaRegFactura", facServPublicDto.FechaRegFactura.ToString("yyyy-MM-dd"));
                    command.Parameters.AddWithValue("@FechaVencimiento", facServPublicDto.FechaVencimiento.ToString("yyyy-MM-dd"));
                    command.Parameters.AddWithValue("@EstadoFactura", facServPublicDto.EstadoFactura ? 1 : 0);
                    command.Parameters.AddWithValue("@IdFactura", facServPublicDto.IdFactura);

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

        public async Task<bool> ConfirmarPagoFacturaServicioPublico(int idFactura)
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

        public async Task<List<FacServPublicDto>> ObtenerHistorialFacturasServiciosPublicosAsync()
        {
            var lista = new List<FacServPublicDto>();
            var idsUsuariosSede = ObtenerIdsUsuariosSedeActual();
            if (idsUsuariosSede.Count == 0) return lista;

            var parametros = idsUsuariosSede.Select((id, idx) => new SqlParameter($"@u{idx}", id)).ToArray();
            string inClause = string.Join(", ", parametros.Select(p => p.ParameterName));

            string sql = $@"SELECT f.id_factura, f.id_usuario, u.nom_usuario AS nom_usuario,
                                   f.tip_factura, f.id_proveedor, f.id_servicio, s.nom_servicio,
                                   f.valor_factura, f.fecha_reg_factura, f.fecha_vencimiento,
                                   f.fecha_pago, f.estado_factura
                            FROM SARABD.dbo.factura f
                            LEFT JOIN SARABD.dbo.usuario u ON f.id_usuario = u.id_usuario
                            LEFT JOIN SARABD.dbo.servicio s ON f.id_servicio = s.id_servicio
                            WHERE f.estado_factura = 0 AND f.id_servicio IS NOT NULL
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
                            lista.Add(new FacServPublicDto
                            {
                                IdFactura = reader["id_factura"] != DBNull.Value ? Convert.ToInt32(reader["id_factura"]) : 0,
                                IdUsuario = reader["id_usuario"] != DBNull.Value ? Convert.ToInt32(reader["id_usuario"]) : 0,
                                NomUsuario = reader["nom_usuario"] != DBNull.Value ? reader["nom_usuario"].ToString() : "N/D",
                                TipFactura = reader["tip_factura"] != DBNull.Value && Convert.ToBoolean(reader["tip_factura"]),
                                IdProveedor = reader["id_proveedor"] != DBNull.Value ? Convert.ToInt32(reader["id_proveedor"]) : (int?)null,
                                IdServicio = reader["id_servicio"] != DBNull.Value ? Convert.ToInt32(reader["id_servicio"]) : (int?)null,
                                NomServicio = reader["nom_servicio"] != DBNull.Value ? reader["nom_servicio"].ToString() : "Sin servicio público",
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
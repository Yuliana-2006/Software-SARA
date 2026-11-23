using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Software_SARA.Dtos;
using Software_SARA.Repositories.Models;
using Software_SARA.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Software_SARA.Repositories
{
    public class FacServPubRepository
    {
        private readonly SarabdContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly Func<IDBSaraUtility> _dbFactory;

        public FacServPubRepository(
            SarabdContext context,
            IHttpContextAccessor httpContextAccessor,
            Func<IDBSaraUtility>? dbFactory = null)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _dbFactory = dbFactory ?? (() => new DBSaraUtility());
        }

        private static void AddParam(DbCommand command, string name, object value)
        {
            var param = command.CreateParameter();
            param.ParameterName = name;
            param.Value = value;
            command.Parameters.Add(param);
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

        public virtual async Task<List<Factura>> ObtenerTodasLasFacturasServiciosPublicos()
        {
            var listaFacturas = new List<Factura>();
            var idsUsuariosSede = ObtenerIdsUsuariosSedeActual();
            if (idsUsuariosSede.Count == 0) return listaFacturas;

            var parametros = idsUsuariosSede.Select((id, idx) => new SqlParameter($"@u{idx}", id)).ToArray();
            string inClause = string.Join(", ", parametros.Select(p => p.ParameterName));

            string sql = $@"SELECT id_factura, id_usuario, tip_factura, id_proveedor,
                           nom_servicio, id_servicio, valor_factura,
                           fecha_reg_factura, fecha_vencimiento, estado_factura
                    FROM dbo.vw_FacturasNombresServiciosActivas
                    WHERE id_usuario IN ({inClause})
                    ORDER BY id_factura DESC;";

            DBSaraUtility connectionUtility = new DBSaraUtility();
            connectionUtility.Connect();
            try
            {
                using (DbCommand command = connectionUtility.GetDbConnection().CreateCommand())
                {
                    command.CommandText = sql;
                    for (int idx = 0; idx < idsUsuariosSede.Count; idx++)
                    {
                        AddParam(command, $"@u{idx}", idsUsuariosSede[idx]);
                    }

                    using (DbDataReader reader = await command.ExecuteReaderAsync())
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

        public virtual async Task<bool> CreateFacturaServicioPublico(FacServPublicDto facServPublicDto)
        {
            bool result = false;
            string sql = "dbo.InsertarFacturaServicio";

            IDBSaraUtility connectionUtility = _dbFactory();
            connectionUtility.Connect();
            try
            {
                using (DbCommand command = connectionUtility.GetDbConnection().CreateCommand())
                {
                    command.CommandText = sql;
                    AddParam(command, "@IdUsuario", facServPublicDto.IdUsuario);
                    AddParam(command, "@TipFactura", facServPublicDto.TipFactura ? 1 : 0);
                    AddParam(command, "@IdProveedor", facServPublicDto.IdProveedor.HasValue ? facServPublicDto.IdProveedor.Value : DBNull.Value);
                    AddParam(command, "@IdServicio", facServPublicDto.IdServicio.HasValue ? facServPublicDto.IdServicio.Value : DBNull.Value);
                    AddParam(command, "@ValorFactura", facServPublicDto.ValorFactura);
                    AddParam(command, "@FechaRegFactura", facServPublicDto.FechaRegFactura.ToString("yyyy-MM-dd"));
                    AddParam(command, "@FechaVencimiento", facServPublicDto.FechaVencimiento.ToString("yyyy-MM-dd"));
                    AddParam(command, "@EstadoFactura", facServPublicDto.EstadoFactura ? 1 : 0);
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

        public virtual async Task<List<SelectListItem>> ObtenerListaServiciosPublicos()
        {
            var listaServiciosPublicos = new List<SelectListItem>();
            string sql = "SELECT id_servicio, nom_servicio FROM dbo.servicio ORDER BY nom_servicio ASC;";

            IDBSaraUtility connectionUtility = _dbFactory();
            connectionUtility.Connect();
            try
            {
                using (DbCommand command = connectionUtility.GetDbConnection().CreateCommand())
                {
                    command.CommandText = sql;
                    using (DbDataReader reader = await command.ExecuteReaderAsync())
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
            }
            finally
            {
                connectionUtility.Disconnect();
            }
            return listaServiciosPublicos;
        }

        public virtual async Task<bool> EditarFacturaServicioPublico(FacServPublicDto facServPublicDto)
        {
            bool result = false;
            string SQL = @"UPDATE dbo.factura
                           SET id_usuario = @IdUsuario,
                               id_proveedor = @IdProveedor,
                               id_servicio = @IdServicio,
                               valor_factura = @ValorFactura,
                               fecha_reg_factura = @FechaRegFactura,
                               fecha_vencimiento = @FechaVencimiento,
                               estado_factura = @EstadoFactura
                           WHERE id_factura = @IdFactura;";

            IDBSaraUtility connectionUtility = _dbFactory();
            connectionUtility.Connect();
            try
            {
                using (DbCommand command = connectionUtility.GetDbConnection().CreateCommand())
                {
                    command.CommandText = SQL;
                    AddParam(command, "@IdUsuario", facServPublicDto.IdUsuario);
                    AddParam(command, "@IdProveedor", facServPublicDto.IdProveedor.HasValue ? facServPublicDto.IdProveedor.Value : DBNull.Value);
                    AddParam(command, "@IdServicio", facServPublicDto.IdServicio.HasValue ? facServPublicDto.IdServicio.Value : DBNull.Value);
                    AddParam(command, "@ValorFactura", facServPublicDto.ValorFactura);
                    AddParam(command, "@FechaRegFactura", facServPublicDto.FechaRegFactura.ToString("yyyy-MM-dd"));
                    AddParam(command, "@FechaVencimiento", facServPublicDto.FechaVencimiento.ToString("yyyy-MM-dd"));
                    AddParam(command, "@EstadoFactura", facServPublicDto.EstadoFactura ? 1 : 0);
                    AddParam(command, "@IdFactura", facServPublicDto.IdFactura);
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

        public virtual async Task<bool> ConfirmarPagoFacturaServicioPublico(int idFactura)
        {
            bool result = false;
            string fechaActual = DateTime.Now.ToString("yyyy-MM-dd");
            string SQL = "UPDATE dbo.factura SET estado_factura = 0, fecha_pago = @FechaPago WHERE id_factura = @IdFactura;";

            IDBSaraUtility connectionUtility = _dbFactory();
            connectionUtility.Connect();
            try
            {
                using (DbCommand command = connectionUtility.GetDbConnection().CreateCommand())
                {
                    command.CommandText = SQL;
                    AddParam(command, "@FechaPago", fechaActual);
                    AddParam(command, "@IdFactura", idFactura);
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

        public virtual async Task<List<FacServPublicDto>> ObtenerHistorialFacturasServiciosPublicosAsync()
        {
            var lista = new List<FacServPublicDto>();
            var idsUsuariosSede = ObtenerIdsUsuariosSedeActual();
            if (idsUsuariosSede.Count == 0) return lista;

            var parametros = idsUsuariosSede.Select((id, idx) => new SqlParameter($"@u{idx}", id)).ToArray();
            string inClause = string.Join(", ", parametros.Select(p => p.ParameterName));

            string sql = $@"SELECT id_factura, id_usuario, nom_usuario,
                           id_servicio, nom_servicio, valor_factura,
                           fecha_reg_factura, fecha_vencimiento, fecha_pago,
                           estado_factura
                    FROM dbo.vw_FacturasServiciosPagoConfirmadoConUsuario
                    WHERE id_usuario IN ({inClause})
                    ORDER BY id_factura DESC;";

            DBSaraUtility connectionUtility = new DBSaraUtility();
            connectionUtility.Connect();
            try
            {
                using (DbCommand command = connectionUtility.GetDbConnection().CreateCommand())
                {
                    command.CommandText = sql;
                    for (int idx = 0; idx < idsUsuariosSede.Count; idx++)
                    {
                        AddParam(command, $"@u{idx}", idsUsuariosSede[idx]);
                    }

                    using (DbDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            lista.Add(new FacServPublicDto
                            {
                                IdFactura = reader["id_factura"] != DBNull.Value ? Convert.ToInt32(reader["id_factura"]) : 0,
                                IdUsuario = reader["id_usuario"] != DBNull.Value ? Convert.ToInt32(reader["id_usuario"]) : 0,
                                NomUsuario = reader["nom_usuario"] != DBNull.Value ? reader["nom_usuario"].ToString() : "N/D",
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
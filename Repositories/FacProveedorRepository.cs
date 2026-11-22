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

namespace Software_SARA.Repositories
{
    public class FacProveedorRepository
    {
        private readonly SarabdContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly Func<IDBSaraUtility> _dbFactory;

        public FacProveedorRepository(
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
                        // Si es Dueño (1) o Administrador (5) y no tiene sede asignada, puede ver todos los usuarios activos
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

        public virtual async Task<List<Factura>> ObtenerTodasLasFacturasProveedores()
        {
            var listaFacturas = new List<Factura>();
            var idsUsuariosSede = ObtenerIdsUsuariosSedeActual();
            if (idsUsuariosSede.Count == 0) return listaFacturas;

            string inClause = string.Join(", ", idsUsuariosSede.Select((_, idx) => $"@u{idx}"));
            string sql = $@"SELECT f.id_factura, f.id_usuario, f.tip_factura, f.id_proveedor,
                                   p.nom_proveedor, f.id_servicio, f.valor_factura,
                                   f.fecha_reg_factura, f.fecha_vencimiento, f.estado_factura
                            FROM SARABD.dbo.factura f
                            LEFT JOIN SARABD.dbo.proveedor p ON f.id_proveedor = p.id_proveedor
                            WHERE f.estado_factura = 1 AND f.id_proveedor IS NOT NULL
                              AND f.id_usuario IN ({inClause})
                            ORDER BY f.id_factura DESC;";

            IDBSaraUtility connectionUtility = _dbFactory();
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

        public virtual async Task<bool> CreateFacturaProveedor(FacProveedorDto facProveedorDto)
        {
            bool result = false;
            string sql = "SARABD.dbo.InsertarFacturaProveedor";

            IDBSaraUtility connectionUtility = _dbFactory();
            connectionUtility.Connect();
            try
            {
                using (DbCommand command = connectionUtility.GetDbConnection().CreateCommand())
                {
                    command.CommandText = sql;
                    AddParam(command, "@IdUsuario", facProveedorDto.IdUsuario);
                    AddParam(command, "@TipFactura", facProveedorDto.TipFactura ? 1 : 0);
                    AddParam(command, "@IdProveedor", facProveedorDto.IdProveedor.HasValue ? facProveedorDto.IdProveedor.Value : DBNull.Value);
                    AddParam(command, "@IdServicio", facProveedorDto.IdServicio.HasValue ? facProveedorDto.IdServicio.Value : DBNull.Value);
                    AddParam(command, "@ValorFactura", facProveedorDto.ValorFactura);
                    AddParam(command, "@FechaRegFactura", facProveedorDto.FechaRegFactura.ToString("yyyy-MM-dd"));
                    AddParam(command, "@FechaVencimiento", facProveedorDto.FechaVencimiento.ToString("yyyy-MM-dd"));
                    AddParam(command, "@EstadoFactura", facProveedorDto.EstadoFactura ? 1 : 0);
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

        public virtual async Task<List<SelectListItem>> ObtenerListaProveedores()
        {
            var listaProveedores = new List<SelectListItem>();
            string sql = "SELECT id_proveedor, nom_proveedor FROM SARABD.dbo.proveedor ORDER BY nom_proveedor ASC;";

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
                            listaProveedores.Add(new SelectListItem
                            {
                                Value = reader["id_proveedor"].ToString(),
                                Text = reader["nom_proveedor"].ToString()
                            });
                        }
                    }
                }
            }
            finally
            {
                connectionUtility.Disconnect();
            }
            return listaProveedores;
        }

        public virtual async Task<bool> EditarFacturaProveedor(FacProveedorDto facProveedorDto)
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

            IDBSaraUtility connectionUtility = _dbFactory();
            connectionUtility.Connect();
            try
            {
                using (DbCommand command = connectionUtility.GetDbConnection().CreateCommand())
                {
                    command.CommandText = SQL;
                    AddParam(command, "@IdUsuario", facProveedorDto.IdUsuario);
                    AddParam(command, "@IdProveedor", facProveedorDto.IdProveedor.HasValue ? facProveedorDto.IdProveedor.Value : DBNull.Value);
                    AddParam(command, "@IdServicio", facProveedorDto.IdServicio.HasValue ? facProveedorDto.IdServicio.Value : DBNull.Value);
                    AddParam(command, "@ValorFactura", facProveedorDto.ValorFactura);
                    AddParam(command, "@FechaRegFactura", facProveedorDto.FechaRegFactura.ToString("yyyy-MM-dd"));
                    AddParam(command, "@FechaVencimiento", facProveedorDto.FechaVencimiento.ToString("yyyy-MM-dd"));
                    AddParam(command, "@EstadoFactura", facProveedorDto.EstadoFactura ? 1 : 0);
                    AddParam(command, "@IdFactura", facProveedorDto.IdFactura);
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

        public virtual async Task<bool> ConfirmarPagoFacturaProveedor(int idFactura)
        {
            bool result = false;
            string fechaActual = DateTime.Now.ToString("yyyy-MM-dd");
            string SQL = "UPDATE SARABD.dbo.factura SET estado_factura = 0, fecha_pago = @FechaPago WHERE id_factura = @IdFactura;";

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

        public virtual async Task<List<FacProveedorDto>> ObtenerHistorialFacturasProveedoresAsync()
        {
            var lista = new List<FacProveedorDto>();
            var idsUsuariosSede = ObtenerIdsUsuariosSedeActual();
            if (idsUsuariosSede.Count == 0) return lista;

            string inClause = string.Join(", ", idsUsuariosSede.Select((_, idx) => $"@u{idx}"));
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

            IDBSaraUtility connectionUtility = _dbFactory();
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
                            lista.Add(new FacProveedorDto
                            {
                                IdFactura = reader["id_factura"] != DBNull.Value ? Convert.ToInt32(reader["id_factura"]) : 0,
                                IdUsuario = reader["id_usuario"] != DBNull.Value ? Convert.ToInt32(reader["id_usuario"]) : 0,
                                NomUsuario = reader["nom_usuario"] != DBNull.Value ? reader["nom_usuario"].ToString() : "N/D",
                                IdProveedor = reader["id_proveedor"] != DBNull.Value ? Convert.ToInt32(reader["id_proveedor"]) : (int?)null,
                                NomProveedor = reader["nom_proveedor"] != DBNull.Value ? reader["nom_proveedor"].ToString() : "Sin Proveedor",
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
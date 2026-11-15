using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Software_SARA.Dtos;
using Software_SARA.Repositories.Models;
using Software_SARA.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data; // IMPORTANTE: Necesario para usar CommandType.StoredProcedure

namespace Software_SARA.Repositories
{
    public class FacProveedorRepository
    {
        public async Task<List<Factura>> ObtenerTodasLasFacturasProveedores()
        {
            var listaFacturas = new List<Factura>();
            string sql = "SELECT * FROM SARABD.dbo.vw_FacturasNombresProveedoresActivas;";

            DBSaraUtility connectionUtility = new DBSaraUtility();
            connectionUtility.Connect();

            using (SqlCommand command = new SqlCommand(sql, connectionUtility.CONN()))
            {
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var factura = new Factura
                        {
                            IdFactura = reader["id_factura"] != DBNull.Value ? Convert.ToInt32(reader["id_factura"]) : 0,
                            IdUsuario = reader["id_usuario"] != DBNull.Value ? Convert.ToInt32(reader["id_usuario"]) : 0,
                            TipFactura = reader["tip_factura"] != DBNull.Value && Convert.ToBoolean(reader["tip_factura"]),
                            IdProveedor = reader["id_proveedor"] != DBNull.Value ? Convert.ToInt32(reader["id_proveedor"]) : 0,
                            NomProveedor = reader["nom_proveedor"] != DBNull.Value ? reader["nom_proveedor"].ToString() : "Sin Proveedor",
                            IdServicio = reader["id_servicio"] != DBNull.Value ? Convert.ToInt32(reader["id_servicio"]) : 0,
                            ValorFactura = reader["valor_factura"] != DBNull.Value ? Convert.ToDecimal(reader["valor_factura"]) : 0,
                            // CONVERSIÓN CORRECTA A DATEONLY
                            FechaRegFactura = reader["fecha_reg_factura"] != DBNull.Value ? DateOnly.FromDateTime(Convert.ToDateTime(reader["fecha_reg_factura"])) : default,
                            FechaVencimiento = reader["fecha_vencimiento"] != DBNull.Value ? DateOnly.FromDateTime(Convert.ToDateTime(reader["fecha_vencimiento"])) : default,
                            EstadoFactura = reader["estado_factura"] != DBNull.Value && Convert.ToBoolean(reader["estado_factura"])
                        };
                        listaFacturas.Add(factura);
                    }
                }
            }
            connectionUtility.Disconnect();
            return listaFacturas;
        }

        public async Task<Boolean> CreateFacturaProveedor(FacProveedorDto facProveedorDto)
        {
            bool result = false;
            
            // SE USÓ LA VERSIÓN DE DEVELOP (PROCEDIMIENTO ALMACENADO)
            DBSaraUtility connectionUtility = new DBSaraUtility();
            connectionUtility.Connect();

            // Usamos el nombre del procedimiento almacenado
            using (SqlCommand command = new SqlCommand("SARABD.dbo.InsertarFacturaProveedor", connectionUtility.CONN()))
            {
                // Indicamos que es un procedimiento almacenado
                command.CommandType = CommandType.StoredProcedure;

                // Añadimos los parámetros de forma segura (Previene Inyección SQL)
                command.Parameters.AddWithValue("@id_usuario", facProveedorDto.IdUsuario);
                command.Parameters.AddWithValue("@tip_factura", facProveedorDto.TipFactura);
                command.Parameters.AddWithValue("@id_proveedor", facProveedorDto.IdProveedor);

                // Manejamos el caso de que IdServicio sea nulo (Nullable)
                command.Parameters.AddWithValue("@id_servicio", (object)facProveedorDto.IdServicio ?? DBNull.Value);

                command.Parameters.AddWithValue("@valor_factura", facProveedorDto.ValorFactura);

                // Convertimos DateOnly a DateTime para SQL Server
                command.Parameters.AddWithValue("@fecha_reg_factura", facProveedorDto.FechaRegFactura.ToDateTime(TimeOnly.MinValue));
                command.Parameters.AddWithValue("@fecha_vencimiento", facProveedorDto.FechaVencimiento.ToDateTime(TimeOnly.MinValue));

                command.Parameters.AddWithValue("@estado_factura", facProveedorDto.EstadoFactura);

                // Ejecución asíncrona segura
                int filasAfectadas = await command.ExecuteNonQueryAsync();
                result = filasAfectadas > 0;
            }

            connectionUtility.Disconnect();
            return result;
        }

        public async Task<List<SelectListItem>> ObtenerListaProveedores()
        {
            var listaProveedores = new List<SelectListItem>();
            string sql = "SELECT id_proveedor, nom_proveedor FROM SARABD.dbo.proveedor ORDER BY nom_proveedor ASC;";
            DBSaraUtility connectionUtility = new DBSaraUtility();
            connectionUtility.Connect();
            using (SqlCommand command = new SqlCommand(sql, connectionUtility.CONN()))
            {
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        listaProveedores.Add(new SelectListItem { Value = reader["id_proveedor"].ToString(), Text = reader["nom_proveedor"].ToString() });
                    }
                }
            }
            connectionUtility.Disconnect();
            return listaProveedores;
        }

        public async Task<Boolean> EditarFacturaProveedor(FacProveedorDto facProveedorDto)
        {
            bool result = false;
            string SQL = @"UPDATE SARABD.dbo.factura SET id_usuario = '" + facProveedorDto.IdUsuario + "', " + "id_proveedor = '" + facProveedorDto.IdProveedor + "', " + "id_servicio = " + (facProveedorDto.IdServicio.HasValue ? "'" + facProveedorDto.IdServicio.Value + "'" : "NULL") + ", " + "valor_factura = '" + facProveedorDto.ValorFactura.ToString(System.Globalization.CultureInfo.InvariantCulture) + "', " + "fecha_reg_factura = '" + facProveedorDto.FechaRegFactura.ToString("yyyy-MM-dd") + "', " + "fecha_vencimiento = '" + facProveedorDto.FechaVencimiento.ToString("yyyy-MM-dd") + "', " + "estado_factura = '" + (facProveedorDto.EstadoFactura ? 1 : 0) + "' " + "WHERE id_factura = " + facProveedorDto.IdFactura + ";";
            DBSaraUtility Connection = new DBSaraUtility();
            Connection.Connect();
            using (SqlCommand command = new SqlCommand(SQL, Connection.CONN()))
            {
                int comando = command.ExecuteNonQuery();
                result = comando != 0;
            }
            Connection.Disconnect();
            return result;
        }

        public async Task<Boolean> ConfirmarPagoFacturaProveedor(int idFactura)
        {
            bool result = false;
            string fechaActual = DateTime.Now.ToString("yyyy-MM-dd");
            string SQL = "UPDATE SARABD.dbo.factura SET estado_factura = 0, fecha_pago = '" + fechaActual + "' WHERE id_factura = " + idFactura + ";";
            DBSaraUtility Connection = new DBSaraUtility();
            Connection.Connect();
            using (SqlCommand command = new SqlCommand(SQL, Connection.CONN()))
            {
                int comando = command.ExecuteNonQuery();
                result = comando != 0;
            }
            Connection.Disconnect();
            return result;
        }

        public async Task<List<HistorialFacturaDto>> ObtenerHistorialFacturasProveedoresAsync()
        {
            // SE USÓ LA VERSIÓN DE DEVELOP (VISTA SQL)
            var lista = new List<HistorialFacturaDto>();
            string sql = "SELECT * FROM SARABD.dbo.vw_FacturasProveedoresPagoConfirmadoConUsuario;";

            DBSaraUtility connectionUtility = new DBSaraUtility();
            connectionUtility.Connect();
            using (SqlCommand command = new SqlCommand(sql, connectionUtility.CONN()))
            {
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        lista.Add(new HistorialFacturaDto
                        {
                            IdFactura = reader["id_factura"] != DBNull.Value ? Convert.ToInt32(reader["id_factura"]) : 0,
                            NomUsuario = reader["nom_usuario"] != DBNull.Value ? reader["nom_usuario"].ToString() : "N/D",
                            NombreEntidad = reader["nom_proveedor"] != DBNull.Value ? reader["nom_proveedor"].ToString() : "Sin Proveedor",
                            ValorFactura = reader["valor_factura"] != DBNull.Value ? Convert.ToDecimal(reader["valor_factura"]) : 0,
                            // CONVERSIÓN CORRECTA A DATEONLY
                            FechaRegFactura = reader["fecha_reg_factura"] != DBNull.Value ? DateOnly.FromDateTime(Convert.ToDateTime(reader["fecha_reg_factura"])) : default,
                            FechaVencimiento = reader["fecha_vencimiento"] != DBNull.Value ? DateOnly.FromDateTime(Convert.ToDateTime(reader["fecha_vencimiento"])) : default,
                            FechaPago = reader["fecha_pago"] != DBNull.Value ? DateOnly.FromDateTime(Convert.ToDateTime(reader["fecha_pago"])) : null,
                            EstadoFactura = reader["estado_factura"] != DBNull.Value && Convert.ToBoolean(reader["estado_factura"])
                        });
                    }
                }
            }
            connectionUtility.Disconnect();
            return lista;
        }
    }
}
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Software_SARA.Dtos;
using Software_SARA.Repositories.Models;
using Software_SARA.Utilities;

namespace Software_SARA.Repositories
{
    public class FacServPubRepository
    {
        private readonly SarabdContext _context;

        public FacServPubRepository(SarabdContext context)
        {
            _context = context;
        }

        public async Task<List<Factura>> ObtenerTodasLasFacturasServiciosPublicos()
        {
            var listaFacturas = new List<Factura>();
            string sql = @"SELECT f.id_factura, f.id_usuario, f.tip_factura, f.id_proveedor, 
                             s.nom_servicio, f.id_servicio, f.valor_factura, 
                             f.fecha_reg_factura, f.fecha_vencimiento, f.estado_factura 
                         FROM SARABD.dbo.factura f
                         LEFT JOIN SARABD.dbo.servicio s ON f.id_servicio = s.id_servicio
                         WHERE f.estado_factura = 1 AND f.tip_factura = 0;";

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
                            NomProveedor = reader["nom_servicio"] != DBNull.Value ? reader["nom_servicio"].ToString() : "Sin servicio público",
                            IdServicio = reader["id_servicio"] != DBNull.Value ? Convert.ToInt32(reader["id_servicio"]) : 0,
                            ValorFactura = reader["valor_factura"] != DBNull.Value ? Convert.ToDecimal(reader["valor_factura"]) : 0,
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

        public async Task<Boolean> CreateFacturaServicioPublico(FacServPublicDto facServPublicDto)
        {
            //Consulta SQL para insertar una nueva factura de servicio público en la base de datos
            bool result = false;
            string SQL = "INSERT INTO SARABD.dbo.factura (id_usuario, tip_factura, id_proveedor, id_servicio, valor_factura, fecha_reg_factura, fecha_vencimiento, estado_factura) VALUES ('" + facServPublicDto.IdUsuario + "', '" + (facServPublicDto.TipFactura ? 1 : 0) + "', " + (facServPublicDto.IdProveedor.HasValue ? "'" + facServPublicDto.IdProveedor.Value + "'" : "NULL") + " , '" + facServPublicDto.IdServicio + "' , '" + facServPublicDto.ValorFactura.ToString(System.Globalization.CultureInfo.InvariantCulture) + "', '" + facServPublicDto.FechaRegFactura.ToString("yyyy-MM-dd") + "', '" + facServPublicDto.FechaVencimiento.ToString("yyyy-MM-dd") + "', '" + (facServPublicDto.EstadoFactura ? 1 : 0) + "');";
            DBSaraUtility Connection = new DBSaraUtility();
            Connection.Connect();
            using (SqlCommand command = new SqlCommand(SQL, Connection.CONN()))
            {
                int comando = command.ExecuteNonQuery();
                if (comando != 0)
                {
                    result = true;
                }
                else
                {
                    result = false;
                }
            }
            Connection.Disconnect();
            return result;
        }

        public async Task<List<SelectListItem>> ObtenerListaServiciosPublicos()
        {
            var listaServiciosPublicos = new List<SelectListItem>();
            string sql = "SELECT id_servicio, nom_servicio FROM SARABD.dbo.servicio ORDER BY nom_servicio ASC;";

            DBSaraUtility connectionUtility = new DBSaraUtility();
            connectionUtility.Connect();

            using (SqlCommand command = new SqlCommand(sql, connectionUtility.CONN()))
            {
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

            connectionUtility.Disconnect();
            return listaServiciosPublicos;
        }

        public async Task<Boolean> EditarFacturaServicioPublico(FacServPublicDto facServPublicDto)
        {
            // Consulta SQL para actualizar los datos de la factura de servicio público existente
            bool result = false;
            string SQL = @"UPDATE SARABD.dbo.factura SET id_usuario = '" + facServPublicDto.IdUsuario + "', " + "id_proveedor = " + (facServPublicDto.IdProveedor.HasValue ? "'" + facServPublicDto.IdProveedor.Value + "'" : "NULL") + ", " + "id_servicio = " + facServPublicDto.IdServicio + ", " + "valor_factura = '" + facServPublicDto.ValorFactura.ToString(System.Globalization.CultureInfo.InvariantCulture) + "', " + "fecha_reg_factura = '" + facServPublicDto.FechaRegFactura.ToString("yyyy-MM-dd") + "', " + "fecha_vencimiento = '" + facServPublicDto.FechaVencimiento.ToString("yyyy-MM-dd") + "', " + "estado_factura = '" + (facServPublicDto.EstadoFactura ? 1 : 0) + "' " + "WHERE id_factura = " + facServPublicDto.IdFactura + ";";

            DBSaraUtility Connection = new DBSaraUtility();
            Connection.Connect();

            using (SqlCommand command = new SqlCommand(SQL, Connection.CONN()))
            {
                int comando = command.ExecuteNonQuery();
                if (comando != 0)
                {
                    result = true;
                }
                else
                {
                    result = false;
                }
            }

            Connection.Disconnect();
            return result;
        }

        public async Task<Boolean> ConfirmarPagoFacturaServicioPublico(int idFactura)
        {
            //Consulta SQL para confirmar el pago de una factura de servicio público en la base de datos
            bool result = false;
            string SQL = "UPDATE SARABD.dbo.factura SET estado_factura = 0 WHERE id_factura = " + idFactura + ";";
            DBSaraUtility Connection = new DBSaraUtility();
            Connection.Connect();
            using (SqlCommand command = new SqlCommand(SQL, Connection.CONN()))
            {
                int comando = command.ExecuteNonQuery();
                if (comando != 0)
                {
                    result = true;
                }
                else
                {
                    result = false;
                }
            }
            Connection.Disconnect();
            return result;
        }
    }
}

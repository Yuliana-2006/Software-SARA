using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Software_SARA.Dtos;
using Software_SARA.Repositories.Models;
using Software_SARA.Utilities;
using System.Data;

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
            string sql = "SELECT * FROM SARABD.dbo.vw_FacturasNombresServiciosActivas;";

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
            //Consulta SQL para insertar una nueva factura de proveedor en la base de datos
            bool result = false;
            DBSaraUtility connectionUtility = new DBSaraUtility();
            connectionUtility.Connect();

            // Usamos el nombre del procedimiento almacenado
            using (SqlCommand command = new SqlCommand("SARABD.dbo.InsertarFacturaServicio", connectionUtility.CONN()))
            {
                // Indicamos que es un procedimiento almacenado
                command.CommandType = CommandType.StoredProcedure;

                // Añadimos los parámetros de forma segura (Previene Inyección SQL)
                command.Parameters.AddWithValue("@id_usuario", facServPublicDto.IdUsuario);
                command.Parameters.AddWithValue("@tip_factura", facServPublicDto.TipFactura);

                // Manejamos el caso de que IdProveedor sea nulo (Nullable)
                command.Parameters.AddWithValue("@id_proveedor", (object)facServPublicDto.IdProveedor ?? DBNull.Value);

                command.Parameters.AddWithValue("@id_servicio", facServPublicDto.IdServicio);

                command.Parameters.AddWithValue("@valor_factura", facServPublicDto.ValorFactura);

                // Convertimos DateOnly a DateTime para SQL Server
                command.Parameters.AddWithValue("@fecha_reg_factura", facServPublicDto.FechaRegFactura.ToDateTime(TimeOnly.MinValue));
                command.Parameters.AddWithValue("@fecha_vencimiento", facServPublicDto.FechaVencimiento.ToDateTime(TimeOnly.MinValue));

                command.Parameters.AddWithValue("@estado_factura", facServPublicDto.EstadoFactura);

                // Ejecución asíncrona segura
                int filasAfectadas = await command.ExecuteNonQueryAsync();
                result = filasAfectadas > 0;
            }

            connectionUtility.Disconnect();
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
            bool result = false;
            string fechaActual = DateTime.Now.ToString("yyyy-MM-dd");
            string SQL = "UPDATE SARABD.dbo.factura SET estado_factura = 0, fecha_pago = '" + fechaActual + "' WHERE id_factura = " + idFactura + ";";
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

        public async Task<List<HistorialFacturaDto>> ObtenerHistorialFacturasServiciosPublicosAsync()
        {
            var lista = new List<HistorialFacturaDto>();
            string sql = "SELECT * FROM SARABD.dbo.vw_FacturasServiciosPagoConfirmadoConUsuario;";

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
                            NombreEntidad = reader["nom_servicio"] != DBNull.Value ? reader["nom_servicio"].ToString() : "Sin ServicioPúblico",
                            ValorFactura = reader["valor_factura"] != DBNull.Value ? Convert.ToDecimal(reader["valor_factura"]) : 0,
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
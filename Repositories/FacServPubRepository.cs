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
            return await _context.Facturas
                .AsNoTracking()
                .Where(f => f.EstadoFactura == true) // Filtra solo las facturas activas
                .Where(f => f.TipFactura == false) // Filtra solo las facturas de servicios públicos
                .ToListAsync();

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

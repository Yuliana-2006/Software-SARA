using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Software_SARA.Dtos;
using Software_SARA.Repositories.Models;
using Software_SARA.Utilities;


namespace Software_SARA.Repositories
{
    public class FacProveedorRepository
    {
        private readonly SarabdContext _context;

        public FacProveedorRepository(SarabdContext context)
        {
            _context = context;
        }

        public async Task<List<Factura>> ObtenerTodasLasFacturasProveedores()
        {
            return await _context.Facturas
                .AsNoTracking()
                .Where(f => f.EstadoFactura == true) // Filtra solo las facturas activas
                .ToListAsync();
                
        }

        public async Task<Boolean> CreateFacturaProveedor(FacProveedorDto facProveedorDto)
        {
            //Consulta SQL para insertar una nueva factura de proveedor en la base de datos
            bool result = false;
            string SQL = "INSERT INTO SARABD.dbo.factura (id_usuario, tip_factura, id_proveedor, id_servicio, valor_factura, fecha_reg_factura, fecha_vencimiento, estado_factura) VALUES ('" + facProveedorDto.IdUsuario + "', '"     + (facProveedorDto.TipFactura ? 1 : 0) + "', '" + facProveedorDto.IdProveedor + "', " + (facProveedorDto.IdServicio.HasValue ? "'" + facProveedorDto.IdServicio.Value + "'" : "NULL") + ", '" + facProveedorDto.ValorFactura.ToString(System.Globalization.CultureInfo.InvariantCulture) + "', '" + facProveedorDto.FechaRegFactura.ToString("yyyy-MM-dd") + "', '" + facProveedorDto.FechaVencimiento.ToString("yyyy-MM-dd") + "', '" + (facProveedorDto.EstadoFactura ? 1 : 0) + "');";
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
                        listaProveedores.Add(new SelectListItem
                        {
                            Value = reader["id_proveedor"].ToString(),
                            Text = reader["nom_proveedor"].ToString()
                        });
                    }
                }
            }

            connectionUtility.Disconnect();
            return listaProveedores;
        }

        public async Task<Boolean> EditarFacturaProveedor(FacProveedorDto facProveedorDto)
        {
            // Consulta SQL para actualizar los datos de la factura de proveedor existente
            bool result = false;
            string SQL = @"UPDATE SARABD.dbo.factura SET id_usuario = '" + facProveedorDto.IdUsuario + "', " + "id_proveedor = '" + facProveedorDto.IdProveedor + "', " + "id_servicio = " + (facProveedorDto.IdServicio.HasValue ? "'" + facProveedorDto.IdServicio.Value + "'" : "NULL") + ", " + "valor_factura = '" + facProveedorDto.ValorFactura.ToString(System.Globalization.CultureInfo.InvariantCulture) + "', " + "fecha_reg_factura = '" + facProveedorDto.FechaRegFactura.ToString("yyyy-MM-dd") + "', " + "fecha_vencimiento = '" + facProveedorDto.FechaVencimiento.ToString("yyyy-MM-dd") + "', " + "estado_factura = '" + (facProveedorDto.EstadoFactura ? 1 : 0) + "' " + "WHERE id_factura = " + facProveedorDto.IdFactura + ";";

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

        public async Task<Boolean> ConfirmarPagoFacturaProveedor(int idFactura)
        {
            //Consulta SQL para confirmar el pago de una factura de proveedor en la base de datos
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

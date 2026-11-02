using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Software_SARA.Dtos;
using Software_SARA.Repositories.Models;
using Software_SARA.Utilities;
using System.Security.Cryptography.Pkcs;

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

                .ToListAsync();
        }

        public async Task<Boolean> CreateFacturaProveedor(FacProveedorDto facProveedorDto)
        {
            //Consulta SQL para insertar una nueva factura de proveedor en la base de datos
            bool result = false;
            string SQL = "INSERT INTO SARABD.dbo.factura (id_usuario, tip_factura, id_proveedor, id_servicio, valor_factura, fecha_reg_factura, fecha_vencimiento, estado_factura) VALUES ('" + facProveedorDto.IdUsuario + "', '" + facProveedorDto.TipFactura + "', '" + facProveedorDto.IdProveedor + "', '" + facProveedorDto.IdServicio + "', '" + facProveedorDto.ValorFactura + "', '" + facProveedorDto.FechaRegFactura + "', '" + facProveedorDto.FechaVencimiento + "', '" + facProveedorDto.EstadoFactura + "');";
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
    }
}

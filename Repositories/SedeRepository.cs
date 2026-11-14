using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Software_SARA.Dtos;
using Software_SARA.Repositories.Models;
using Software_SARA.Utilities;
using System.Data;
using System.Security.Cryptography.Pkcs;

namespace Software_SARA.Repositories
{
    public class SedeRepository
    {
        private readonly SarabdContext _context;

        public SedeRepository(SarabdContext context)
        {
            _context = context;
        }

        public async Task<List<Sede>> ObtenerTodasLasSedes()
        {
            return await _context.Sedes
                .AsNoTracking()
                
                .ToListAsync();
        }

        public async Task<Boolean> CreateSede(SedeDto sedeDto)
        {
            //Consulta SQL para insertar una nueva sede en la base de datos
            bool result = false;
            DBSaraUtility connectionUtility = new DBSaraUtility();
            connectionUtility.Connect();

            // Usamos el nombre del procedimiento almacenado
            using (SqlCommand command = new SqlCommand("SARABD.dbo.InsertarSede", connectionUtility.CONN()))
            {
                // Indicamos que es un procedimiento almacenado
                command.CommandType = CommandType.StoredProcedure;

                // Añadimos los parámetros de forma segura (Previene Inyección SQL)
                command.Parameters.AddWithValue("@nom_sede", (object)sedeDto.NomSede ?? DBNull.Value);
                command.Parameters.AddWithValue("@dir_sede", (object)sedeDto.DirSede ?? DBNull.Value);
                command.Parameters.AddWithValue("@tel_sede", (object)sedeDto.TelSede ?? DBNull.Value);
                command.Parameters.AddWithValue("@estado_sede", sedeDto.EstadoSede);
                command.Parameters.AddWithValue("@ciu_sede", (object)sedeDto.CiuSede ?? DBNull.Value);
                command.Parameters.AddWithValue("@dep_sede", (object)sedeDto.DepSede ?? DBNull.Value);
                command.Parameters.AddWithValue("@barrio_sede", (object)sedeDto.BarrioSede ?? DBNull.Value);

                // Ejecución asíncrona segura
                int filasAfectadas = await command.ExecuteNonQueryAsync();
                result = filasAfectadas > 0;
            }

            connectionUtility.Disconnect();
            return result;
        }

        public async Task<Boolean> EditarSede(SedeDto sedeDto)
        {
            //Consulta SQL para editar una sede en la base de datos
            bool result = false;
            string SQL = "UPDATE SARABD.dbo.Sede SET nom_sede = '" + sedeDto.NomSede + "', dir_sede = '" + sedeDto.DirSede + "', tel_sede = '" + sedeDto.TelSede + "', estado_sede = '" + sedeDto.EstadoSede + "', ciu_sede = '" + sedeDto.CiuSede + "', dep_sede = '" + sedeDto.DepSede + "', barrio_sede = '" + sedeDto.BarrioSede + "' WHERE id_sede = " + sedeDto.IdSede + ";";
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

        public async Task<Boolean> EliminarSede(int idSede)
        {
            //Consulta SQL para eliminar una sede en la base de datos
            bool result = false;
            string SQL = "UPDATE SARABD.dbo.sede SET estado_sede = 0 WHERE id_sede = " + idSede + ";";
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

        public async Task<bool> ReactivarSede(int idSede)
        {
            bool result = false;
            string SQL = "UPDATE SARABD.dbo.sede SET estado_sede = 1 WHERE id_sede = " + idSede + ";";

            DBSaraUtility Connection = new DBSaraUtility();
            Connection.Connect();

            using (SqlCommand command = new SqlCommand(SQL, Connection.CONN()))
            {
                int comando = command.ExecuteNonQuery();
                if (comando != 0)
                {
                    result = true;
                }
            }

            Connection.Disconnect();
            return result;
        }
    }
}

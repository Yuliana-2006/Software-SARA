using Software_SARA.Dtos;
using Software_SARA.Utilities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.Pkcs;
using Software_SARA.Repositories.Models;

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
                .Where(sede => sede.EstadoSede)
                .ToListAsync();
        }

        public async Task<Boolean> CreateSede(SedeDto sedeDto)
        {
            //Consulta SQL para insertar una nueva sede en la base de datos
            bool result = false;
            string SQL = "INSERT INTO SARABD.dbo.Sede (nom_sede, dir_sede, tel_sede, estado_sede, ciu_sede, dep_sede, barrio_sede) VALUES ('" + sedeDto.NomSede + "', '" + sedeDto.DirSede + "', '" + sedeDto.TelSede + "', '" + sedeDto.EstadoSede + "', '" + sedeDto.CiuSede + "', '" + sedeDto.DepSede + "', '" + sedeDto.BarrioSede + "');";
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
    }
}

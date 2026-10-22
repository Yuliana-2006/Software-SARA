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
            string SQL = "INSERT INTO SARABD.dbo.Sede (NomSede, DirSede, TelSede, EstadoSede, CiuSede, DepSede, BarrioSede) VALUES ('" + sedeDto.NomSede + "', '" + sedeDto.DirSede + "', '" + sedeDto.TelSede + "', '" + sedeDto.EstadoSede + "', '" + sedeDto.CiuSede + "', '" + sedeDto.DepSede + "', '" + sedeDto.BarrioSede + "');";
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

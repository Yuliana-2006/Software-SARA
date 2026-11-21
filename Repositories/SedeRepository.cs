using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Software_SARA.Dtos;
using Software_SARA.Repositories.Models;
using Software_SARA.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Software_SARA.Repositories
{
    public class SedeRepository
    {
        private readonly SarabdContext _context;

        public SedeRepository(SarabdContext context)
        {
            _context = context;
        }

        public async Task<bool> TieneInformacionAsociadaAsync(int idSede)
        {
            var idsUsuariosSede = await _context.Usuarios
                .Where(u => u.IdSede == idSede)
                .Select(u => u.IdUsuario)
                .ToListAsync();

            if (idsUsuariosSede.Count == 0)
                return false;

            bool tieneFacturas = await _context.Facturas
                .AnyAsync(f => idsUsuariosSede.Contains(f.IdUsuario));

            bool tieneRecordatorios = await _context.Recordatorios
                .AnyAsync(r => idsUsuariosSede.Contains(r.IdUsuario));

            bool tieneCajas = await _context.DiaCajas
                .AnyAsync(c => idsUsuariosSede.Contains(c.IdUsuario) ||
                              (c.IdUsuarioCierre.HasValue && idsUsuariosSede.Contains(c.IdUsuarioCierre.Value)));

            return tieneFacturas || tieneRecordatorios || tieneCajas;
        }

        public async Task<List<Sede>> ObtenerTodasLasSedes()
        {
            return await _context.Sedes
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<bool> CreateSede(SedeDto sedeDto)
        {
            bool result = false;
            string sql = "dbo.InsertarSede";

            DBSaraUtility connection = new DBSaraUtility();
            connection.Connect();
            try
            {
                using (SqlCommand command = new SqlCommand(sql, connection.CONN()))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@nom_sede", sedeDto.NomSede ?? string.Empty);
                    command.Parameters.AddWithValue("@dir_sede", sedeDto.DirSede ?? string.Empty);
                    command.Parameters.AddWithValue("@tel_sede", sedeDto.TelSede ?? string.Empty);
                    command.Parameters.AddWithValue("@estado_sede", sedeDto.EstadoSede);
                    command.Parameters.AddWithValue("@ciu_sede", sedeDto.CiuSede ?? string.Empty);
                    command.Parameters.AddWithValue("@dep_sede", sedeDto.DepSede ?? string.Empty);
                    command.Parameters.AddWithValue("@barrio_sede", sedeDto.BarrioSede ?? string.Empty);

                    int comando = await command.ExecuteNonQueryAsync();
                    result = comando != 0;
                }
            }
            finally
            {
                connection.Disconnect();
            }
            return result;
        }

        public async Task<bool> EditarSede(SedeDto sedeDto)
        {
            bool result = false;
            string sql = @"UPDATE SARABD.dbo.Sede
                           SET nom_sede = @NomSede,
                               dir_sede = @DirSede,
                               tel_sede = @TelSede,
                               estado_sede = @EstadoSede,
                               ciu_sede = @CiuSede,
                               dep_sede = @DepSede,
                               barrio_sede = @BarrioSede
                           WHERE id_sede = @IdSede;";

            DBSaraUtility connection = new DBSaraUtility();
            connection.Connect();
            try
            {
                using (SqlCommand command = new SqlCommand(sql, connection.CONN()))
                {
                    command.Parameters.AddWithValue("@NomSede", sedeDto.NomSede ?? string.Empty);
                    command.Parameters.AddWithValue("@DirSede", sedeDto.DirSede ?? string.Empty);
                    command.Parameters.AddWithValue("@TelSede", sedeDto.TelSede ?? string.Empty);
                    command.Parameters.AddWithValue("@EstadoSede", sedeDto.EstadoSede ? 1 : 0);
                    command.Parameters.AddWithValue("@CiuSede", sedeDto.CiuSede ?? string.Empty);
                    command.Parameters.AddWithValue("@DepSede", sedeDto.DepSede ?? string.Empty);
                    command.Parameters.AddWithValue("@BarrioSede", sedeDto.BarrioSede ?? string.Empty);
                    command.Parameters.AddWithValue("@IdSede", sedeDto.IdSede);

                    int comando = await command.ExecuteNonQueryAsync();
                    result = comando != 0;
                }
            }
            finally
            {
                connection.Disconnect();
            }
            return result;
        }

        public async Task<bool> EliminarSede(int idSede)
        {
            if (await TieneInformacionAsociadaAsync(idSede))
            {
                return false;
            }

            bool result = false;
            string sql = "UPDATE SARABD.dbo.sede SET estado_sede = 0 WHERE id_sede = @IdSede;";

            DBSaraUtility connection = new DBSaraUtility();
            connection.Connect();
            try
            {
                using (SqlCommand command = new SqlCommand(sql, connection.CONN()))
                {
                    command.Parameters.AddWithValue("@IdSede", idSede);
                    int comando = await command.ExecuteNonQueryAsync();
                    result = comando != 0;
                }
            }
            finally
            {
                connection.Disconnect();
            }
            return result;
        }

        public async Task<bool> ReactivarSede(int idSede)
        {
            bool result = false;
            string sql = "UPDATE SARABD.dbo.sede SET estado_sede = 1 WHERE id_sede = @IdSede;";

            DBSaraUtility connection = new DBSaraUtility();
            connection.Connect();
            try
            {
                using (SqlCommand command = new SqlCommand(sql, connection.CONN()))
                {
                    command.Parameters.AddWithValue("@IdSede", idSede);
                    int comando = await command.ExecuteNonQueryAsync();
                    result = comando != 0;
                }
            }
            finally
            {
                connection.Disconnect();
            }
            return result;
        }
    }
}
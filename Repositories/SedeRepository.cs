using Microsoft.EntityFrameworkCore;
using Software_SARA.Dtos;
using Software_SARA.Repositories.Models;
using Software_SARA.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace Software_SARA.Repositories
{
    public class SedeRepository
    {
        private readonly SarabdContext _context;
        private readonly Func<IDBSaraUtility> _dbFactory;

        public SedeRepository(SarabdContext context, Func<IDBSaraUtility>? dbFactory = null)
        {
            _context = context;
            _dbFactory = dbFactory ?? (() => new DBSaraUtility());
        }

        private static void AddParam(DbCommand command, string name, object value)
        {
            var param = command.CreateParameter();
            param.ParameterName = name;
            param.Value = value;
            command.Parameters.Add(param);
        }

        public virtual async Task<bool> TieneInformacionAsociadaAsync(int idSede)
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

        public virtual async Task<List<Sede>> ObtenerTodasLasSedes()
        {
            return await _context.Sedes
                .AsNoTracking()
                .ToListAsync();
        }

        public virtual async Task<bool> CreateSede(SedeDto sedeDto)
        {
            bool result = false;
            string sql = @"INSERT INTO SARABD.dbo.Sede
                           (nom_sede, dir_sede, tel_sede, estado_sede, ciu_sede, dep_sede, barrio_sede)
                           VALUES (@NomSede, @DirSede, @TelSede, @EstadoSede, @CiuSede, @DepSede, @BarrioSede);";
            IDBSaraUtility connection = _dbFactory();
            connection.Connect();
            try
            {
                using (DbCommand command = connection.GetDbConnection().CreateCommand())
                {
                    command.CommandText = sql;
                    AddParam(command, "@NomSede", sedeDto.NomSede ?? string.Empty);
                    AddParam(command, "@DirSede", sedeDto.DirSede ?? string.Empty);
                    AddParam(command, "@TelSede", sedeDto.TelSede ?? string.Empty);
                    AddParam(command, "@EstadoSede", sedeDto.EstadoSede ? 1 : 0);
                    AddParam(command, "@CiuSede", sedeDto.CiuSede ?? string.Empty);
                    AddParam(command, "@DepSede", sedeDto.DepSede ?? string.Empty);
                    AddParam(command, "@BarrioSede", sedeDto.BarrioSede ?? string.Empty);
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

        public virtual async Task<bool> EditarSede(SedeDto sedeDto)
        {
            bool result = false;
            string sql = @"UPDATE dbo.Sede
                           SET nom_sede = @NomSede,
                               dir_sede = @DirSede,
                               tel_sede = @TelSede,
                               estado_sede = @EstadoSede,
                               ciu_sede = @CiuSede,
                               dep_sede = @DepSede,
                               barrio_sede = @BarrioSede
                           WHERE id_sede = @IdSede;";
            IDBSaraUtility connection = _dbFactory();
            connection.Connect();
            try
            {
                using (DbCommand command = connection.GetDbConnection().CreateCommand())
                {
                    command.CommandText = sql;
                    AddParam(command, "@NomSede", sedeDto.NomSede ?? string.Empty);
                    AddParam(command, "@DirSede", sedeDto.DirSede ?? string.Empty);
                    AddParam(command, "@TelSede", sedeDto.TelSede ?? string.Empty);
                    AddParam(command, "@EstadoSede", sedeDto.EstadoSede ? 1 : 0);
                    AddParam(command, "@CiuSede", sedeDto.CiuSede ?? string.Empty);
                    AddParam(command, "@DepSede", sedeDto.DepSede ?? string.Empty);
                    AddParam(command, "@BarrioSede", sedeDto.BarrioSede ?? string.Empty);
                    AddParam(command, "@IdSede", sedeDto.IdSede);
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

        public virtual async Task<bool> EliminarSede(int idSede)
        {
            if (await TieneInformacionAsociadaAsync(idSede))
            {
                return false;
            }
            bool result = false;
            string sql = "UPDATE dbo.sede SET estado_sede = 0 WHERE id_sede = @IdSede;";

            DBSaraUtility connection = new DBSaraUtility();
            connection.Connect();
            try
            {
                using (DbCommand command = connection.GetDbConnection().CreateCommand())
                {
                    command.CommandText = sql;
                    AddParam(command, "@IdSede", idSede);
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

        public virtual async Task<bool> ReactivarSede(int idSede)
        {
            bool result = false;
            string sql = "UPDATE dbo.sede SET estado_sede = 1 WHERE id_sede = @IdSede;";

            DBSaraUtility connection = new DBSaraUtility();
            connection.Connect();
            try
            {
                using (DbCommand command = connection.GetDbConnection().CreateCommand())
                {
                    command.CommandText = sql;
                    AddParam(command, "@IdSede", idSede);
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
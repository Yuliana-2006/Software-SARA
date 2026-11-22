using Microsoft.Data.SqlClient;
using System;
using System.Data.Common;

namespace Software_SARA.Utilities
{
    public interface IDBSaraUtility
    {
        void Connect();
        void Disconnect();
        DbConnection GetDbConnection();
    }

    public class DBSaraUtility : IDBSaraUtility
    {
        static string SERVER = "DESKTOP-YULIANA";
        static string DB_NAME = "SARABD";
        static string Conn = "Server=" + SERVER + ";Database=" + DB_NAME + ";Trusted_Connection=True;TrustServerCertificate=True;";
        SqlConnection Con = new SqlConnection(Conn);

        public void Connect()
        {
            try
            {
                Con.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al abrir la conexión: " + ex.Message);
                throw;
            }
        }

        public void Disconnect()
        {
            try
            {
                Con.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al cerrar la conexión: " + ex.Message);
                throw;
            }
        }

        public SqlConnection CONN()
        {
            return Con;
        }

        public DbConnection GetDbConnection()
        {
            return Con;
        }
    }
}
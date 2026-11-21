using System;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Data.SqlClient;

namespace Software_SARA.Utilities
{
    public class DBSaraUtility
    {
        // --- DATOS DE MONSTERASP ---
        static string SERVER = "db81718.databaseasp.net";
        static string DB_NAME = "SARAPRUEBA"; 
        
        // El usuario en MonsterASP suele ser el mismo ID del servidor: db81718
        static string USER = "db81718"; 
        
        // Pon aquí la contraseña real que te dio MonsterASP
        static string PASSWORD = "3229634208"; 

        // Cadena de conexión corregida (usando comillas y variables)
        static string Conn = $"Server={SERVER};Database={DB_NAME};User Id={USER};Password={PASSWORD};Encrypt=False;MultipleActiveResultSets=True;";

        // Conexión
        SqlConnection Con = new SqlConnection(Conn);

        // Procedimiento para abrir la conexión a la base de datos
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

        // Procedimiento para cerrar la conexión a la base de datos
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

        // Función que retorna la conexión a la base de datos
        public SqlConnection CONN()
        {
            return Con;
        }
    }
}
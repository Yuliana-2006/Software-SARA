using Microsoft.Data.SqlClient;

namespace Software_SARA.Utilities
{
    public class DBSaraUtility
    {
        static string SERVER = "LAPTOP-OG1EFTPF";

        static string DB_NAME = "SARABD";

        static string DB_USER = "SARA_app";

        static string DB_PASSWORD = "123";

        static string Conn = "Server=" + SERVER + ";Database=" + DB_NAME + ";Trusted_Connection=True;TrustServerCertificate=True;";
        //mi conxion:

        SqlConnection Con = new SqlConnection(Conn);

        //procedimiento para abrir la conexion a la base de datos
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

        //procedimiento para cerrar la conexion a la base de datos
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

        //Funcion que retorna la conexion a la base de datos
        public SqlConnection CONN()
        {
            return Con;

        }
    }
}

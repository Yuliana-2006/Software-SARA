using BCrypt.Net;

namespace Software_SARA.Utilities
{
    public static class PasswordHelper
    {
        // Encripta una contraseña en texto plano
        public static string Encriptar(string passwordPlano)
        {
            return BCrypt.Net.BCrypt.HashPassword(passwordPlano);
        }

        // Verifica si una contraseña en texto plano coincide con el hash guardado
        public static bool Verificar(string passwordPlano, string passwordHash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(passwordPlano, passwordHash);
            }
            catch
            {
                return false;
            }
        }

        // Genera una contraseña aleatoria de 8 caracteres
        public static string GenerarPassword(int longitud = 8)
        {
            const string caracteres = "ABCDEFGHJKMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";
            var random = new Random();
            return new string(Enumerable.Repeat(caracteres, longitud)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
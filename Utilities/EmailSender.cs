using System.Net;
using System.Net.Mail;

namespace Software_SARA.Utilities
{
    public static class EmailSender
    {
        public static void EnviarCorreo(string destinatario, string asunto, string mensajeHtml)
        {
            // Nota: Debes reemplazar estos datos con tu correo real y tu contraseña de aplicación de Gmail
            string remitenteCorreo = "tucorreo@gmail.com";
            string remitentePassword = "tu_contraseña_de_aplicacion";

            var client = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(remitenteCorreo, remitentePassword)
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(remitenteCorreo, "SARA - Supermercado"),
                Subject = asunto,
                Body = mensajeHtml,
                IsBodyHtml = true,
            };

            mailMessage.To.Add(destinatario);
            client.Send(mailMessage);
        }
    }
}
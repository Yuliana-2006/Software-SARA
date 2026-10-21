using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace Software_SARA.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task EnviarPasswordAsync(string destinatario, string nombre, string password)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(
                _config["EmailSettings:FromName"],
                _config["EmailSettings:FromEmail"]));
            email.To.Add(new MailboxAddress(nombre, destinatario));
            email.Subject = "Bienvenido a SARA - Tus credenciales de acceso";

            // DISEÑO HTML DEL CORREO
            string htmlBody = $@"
            <div style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px;'>
                <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 10px; overflow: hidden; box-shadow: 0 4px 10px rgba(0,0,0,0.1);'>
                    
                    <!-- Encabezado Verde -->
                    <div style='background-color: #2E4F3E; padding: 20px; text-align: center;'>
                        <!-- Si tienes el logo en internet, descomenta esta línea y pon la URL -->
                        <!-- <img src='https://tu-sitio.com/logo-sara.png' alt='SARA Logo' style='width: 150px;'> -->
                        <h1 style='color: #ffffff; margin: 0; font-size: 28px; letter-spacing: 2px;'>SARA</h1>
                        <p style='color: #a0c3a8; margin: 5px 0 0 0; font-size: 14px;'>Alerta y Gestión para tu Supermercado</p>
                    </div>

                    <!-- Cuerpo del Mensaje -->
                    <div style='padding: 30px; color: #333333;'>
                        <h2 style='color: #2E4F3E;'>¡Hola, {nombre}!</h2>
                        <p style='font-size: 16px; line-height: 1.5;'>Tu cuenta ha sido creada exitosamente por el Administrador del sistema.</p>
                        
                        <p style='font-size: 16px; line-height: 1.5;'>Estas son tus credenciales de acceso a la plataforma:</p>
                        
                        <!-- Cuadro de Credenciales -->
                        <div style='background-color: #ECE9DF; border-left: 5px solid #2E4F3E; padding: 15px; margin: 20px 0; border-radius: 5px;'>
                            <p style='margin: 5px 0; font-size: 16px;'><strong>Usuario (Correo):</strong> {destinatario}</p>
                            <p style='margin: 5px 0; font-size: 16px;'><strong>Contraseña temporal:</strong> 
                                <span style='background-color: #2E4F3E; color: white; padding: 5px 10px; border-radius: 5px; font-family: monospace; font-size: 18px; letter-spacing: 2px;'>{password}</span>
                            </p>
                        </div>

                        <p style='font-size: 14px; color: #666666;'>Bienvenido al sistema SARA
                        <div style='text-align: center; margin-top: 30px;'>
                            <a href='https://localhost:7173/Acceso/Login' style='background-color: #2E4F3E; color: #ffffff; text-decoration: none; padding: 12px 25px; border-radius: 25px; font-weight: bold; font-size: 16px;'>Ingresar a SARA</a>
                        </div>
                    </div>

                    <!-- Pie de Página -->
                    <div style='background-color: #f9f9f9; padding: 15px; text-align: center; font-size: 12px; color: #888888;'>
                        <p style='margin: 0;'>Este es un correo automático, por favor no responder.</p>
                        <p style='margin: 5px 0 0 0;'>&copy; 2026 Software SARA - Universidad de Cundinamarca</p>
                    </div>
                </div>
            </div>";

            email.Body = new TextPart("html") { Text = htmlBody };

            using var smtp = new MailKit.Net.Smtp.SmtpClient();
            await smtp.ConnectAsync(
                _config["EmailSettings:Host"],
                int.Parse(_config["EmailSettings:Port"]),
                SecureSocketOptions.StartTls);

            await smtp.AuthenticateAsync(
                _config["EmailSettings:User"],
                _config["EmailSettings:Password"]);

            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}
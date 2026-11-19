using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Software_SARA.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        // 1. ENVIAR CONTRASEÑA (CON LOGO Y NOTA SOLO PARA DUEÑO Y GERENTE)
        public async Task EnviarPasswordAsync(string destinatario, string nombre, string password, string rol, string numDocumento)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_config["EmailSettings:FromName"], _config["EmailSettings:FromEmail"]));
            email.To.Add(new MailboxAddress(nombre, destinatario));
            email.Subject = $"Bienvenido a SARA - Tu cuenta como {rol}";

            string rolNormalizado = rol.Trim().ToLower();

            string mensajeRol = rolNormalizado switch
            {
                "dueño" => "Ahora eres el <strong>Dueño</strong> del sistema SARA. Tienes el control total para gestionar tus sedes y gerentes.",
                "gerente" => "Ahora eres un <strong>Gerente</strong> de sede en el sistema SARA. ¡Estás a cargo de la operación de tu supermercado!",
                "tesorero" => "Ahora haces parte del equipo de <strong>Tesorería</strong> en el sistema SARA. Tu gestión financiera es clave para nosotros.",
                "empleado" or "trabajador" => "Ahora haces parte del equipo de <strong>Trabajadores</strong> en el sistema SARA. ¡Gracias por unirte a bordo!",
                _ => $"Tu cuenta ha sido creada con el rol de <strong>{rol}</strong> en el sistema SARA."
            };

            string notaHtml = (rolNormalizado == "gerente" || rolNormalizado == "dueño")
                ? @"<div style='background-color: #fff3cd; border: 1px solid #ffeeba; color: #856404; padding: 15px; border-radius: 5px; margin-bottom: 20px;'>
                        <p style='margin: 0; font-size: 15px;'><strong>Nota importante:</strong> Recuerda que también se aceptan <strong>cédulas extranjeras</strong> para iniciar sesión.</p>
                    </div>"
                : "";

            var builder = new BodyBuilder();
            string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "logo.png");
            string logoHtml;

            if (File.Exists(imagePath))
            {
                var image = builder.LinkedResources.Add(imagePath);
                image.ContentId = "sara_logo_cid";
                logoHtml = "<img src='cid:sara_logo_cid' alt='SARA Logo' style='width: 160px; max-width: 100%; height: auto; display: block; margin: 0 auto 10px auto;'>";
            }
            else
            {
                logoHtml = "<h1 style='color: #ffffff; margin: 0 0 10px 0; font-size: 32px; letter-spacing: 2px;'>SARA</h1>";
            }

            builder.HtmlBody = $@"
            <div style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px;'>
                <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 10px; overflow: hidden; box-shadow: 0 4px 10px rgba(0,0,0,0.1);'>
                    <div style='background-color: #2E4F3E; padding: 25px; text-align: center;'>
                        {logoHtml}
                        <p style='color: #a0c3a8; margin: 0; font-size: 14px;'>Alerta y Gestión para tu Supermercado</p>
                    </div>
                    <div style='padding: 30px; color: #333333;'>
                        <h2 style='color: #2E4F3E;'>¡Hola, {nombre}!</h2>
                        <p style='font-size: 16px; line-height: 1.5; background-color: #e8f4ea; padding: 15px; border-radius: 5px; border-left: 4px solid #2E4F3E;'>{mensajeRol}</p>
                        <p style='font-size: 16px; line-height: 1.5;'>Estas son tus credenciales de acceso a la plataforma:</p>
                        <div style='background-color: #ECE9DF; border-left: 5px solid #2E4F3E; padding: 15px; margin: 20px 0; border-radius: 5px;'>
                            <p style='margin: 5px 0; font-size: 16px;'><strong>Ingresa con tu número de documento:</strong> {numDocumento}</p>
                            <p style='margin: 5px 0; font-size: 16px;'><strong>Esta es tu contraseña:</strong> 
                                <span style='background-color: #2E4F3E; color: white; padding: 5px 10px; border-radius: 5px; font-family: monospace; font-size: 18px; letter-spacing: 2px;'>{password}</span>
                            </p>
                        </div>
                        {notaHtml}
                        <div style='text-align: center; margin-top: 30px;'>
                            <a href='https://localhost:7173/Acceso/Login' style='background-color: #2E4F3E; color: #ffffff; text-decoration: none; padding: 12px 25px; border-radius: 25px; font-weight: bold; font-size: 16px;'>Ingresar a SARA</a>
                        </div>
                    </div>
                    <div style='background-color: #f9f9f9; padding: 15px; text-align: center; font-size: 12px; color: #888888;'>
                        <p style='margin: 0;'>Este es un correo automático, por favor no responder.</p>
                        <p style='margin: 5px 0 0 0;'>&copy; 2026 Software SARA - Universidad de Cundinamarca</p>
                    </div>
                </div>
            </div>";

            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            smtp.Timeout = 15000;
            await smtp.ConnectAsync(_config["EmailSettings:Host"], int.Parse(_config["EmailSettings:Port"]), SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_config["EmailSettings:User"], _config["EmailSettings:Password"]);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }

        // 2. ENVIAR INFORME (SIN LOGO, Y CON NOMBRE CORRECTO)
        public async Task EnviarInformeCajaAsync(string destinatario, string nombreDestinatario, string sede, DateTime fecha, byte[] pdfBytes)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_config["EmailSettings:FromName"], _config["EmailSettings:FromEmail"]));
            email.To.Add(new MailboxAddress(nombreDestinatario, destinatario));
            email.Subject = $"Informe Diario de Caja - {sede} - {fecha:dd/MM/yyyy}";

            var builder = new BodyBuilder();
            builder.HtmlBody = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px;'>
                    <h2 style='color: #2E4F3E;'>Informe Diario de Caja</h2>
                    <p>Hola <strong>{nombreDestinatario}</strong>,</p>
                    <p>Adjunto encontrarás el informe consolidado de los movimientos de caja para la sede <b>{sede}</b> correspondientes al día <b>{fecha:dd/MM/yyyy}</b>.</p>
                    <br>
                    <p>Atentamente,<br/>Sistema SARA</p>
                </div>";

            builder.Attachments.Add($"InformeCaja_{fecha:yyyyMMdd}.pdf", pdfBytes, new ContentType("application", "pdf"));
            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            smtp.Timeout = 15000;
            await smtp.ConnectAsync(_config["EmailSettings:Host"], int.Parse(_config["EmailSettings:Port"]), SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_config["EmailSettings:User"], _config["EmailSettings:Password"]);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}
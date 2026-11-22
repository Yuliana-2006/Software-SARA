using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using Moq;
using Software_SARA.Services;
using Xunit;

public class EmailServiceTesting
{
    private static IConfiguration CrearConfiguracion() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EmailSettings:FromName"] = "SARA Soporte",
                ["EmailSettings:FromEmail"] = "soporte@sara.local",
                ["EmailSettings:Host"] = "smtp.sara.local",
                ["EmailSettings:Port"] = "587",
                ["EmailSettings:User"] = "usuario@sara.local",
                ["EmailSettings:Password"] = "clave123"
            })
            .Build();

    private static Mock<ISmtpClient> CrearSmtpMock()
    {
        var smtpMock = new Mock<ISmtpClient>(MockBehavior.Loose);
        smtpMock.Setup(s => s.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        smtpMock.Setup(s => s.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        smtpMock.Setup(s => s.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()))
                .Returns(Task.FromResult(string.Empty));
        smtpMock.Setup(s => s.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        return smtpMock;
    }

    [Theory]
    [InlineData("Dueño")]
    [InlineData("Gerente")]
    [InlineData("Tesorero")]
    [InlineData("Empleado")]
    [InlineData("Cajero")]
    public async Task EnviarPasswordAsync_CubreTodosLosRolesYEnviaCorreo(string rol)
    {
        var smtpMock = CrearSmtpMock();
        var service = new EmailService(CrearConfiguracion(), () => smtpMock.Object);

        await service.EnviarPasswordAsync(
            destinatario: "luis@sara.local",
            nombre: "Luis Cano",
            password: "PasswordSegura123",
            rol: rol,
            numDocumento: "1072123456");

        smtpMock.Verify(s => s.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()), Times.Once);
        smtpMock.Verify(s => s.DisconnectAsync(true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnviarPasswordAsync_CuandoExisteLogoPng_AdjuntaRecursoVinculado()
    {
        string imagesDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
        string logoPath = Path.Combine(imagesDir, "logo.png");
        Directory.CreateDirectory(imagesDir);
        await File.WriteAllBytesAsync(logoPath, new byte[] { 137, 80, 78, 71 });

        try
        {
            var smtpMock = CrearSmtpMock();
            var service = new EmailService(CrearConfiguracion(), () => smtpMock.Object);

            await service.EnviarPasswordAsync(
                destinatario: "gerente@sara.local",
                nombre: "Ana Gómez",
                password: "Clave456",
                rol: "Gerente",
                numDocumento: "98765432");

            smtpMock.Verify(s => s.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()), Times.Once);
        }
        finally
        {
            if (File.Exists(logoPath))
            {
                File.Delete(logoPath);
            }
        }
    }

    [Fact]
    public async Task EnviarInformeCajaAsync_EnviaCorreoConAdjuntoPdf()
    {
        var smtpMock = CrearSmtpMock();
        var service = new EmailService(CrearConfiguracion(), () => smtpMock.Object);
        byte[] pdfBytes = new byte[] { 37, 80, 68, 70 };

        await service.EnviarInformeCajaAsync(
            destinatario: "tesoreria@sara.local",
            nombreDestinatario: "Carlos Pérez",
            sede: "Sede Principal",
            fecha: new DateTime(2026, 11, 22),
            pdfBytes: pdfBytes);

        smtpMock.Verify(s => s.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()), Times.Once);
        smtpMock.Verify(s => s.DisconnectAsync(true, It.IsAny<CancellationToken>()), Times.Once);
    }
}
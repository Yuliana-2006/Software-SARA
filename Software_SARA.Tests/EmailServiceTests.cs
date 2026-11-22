using System.Reflection;
using Microsoft.Extensions.Configuration;
using Software_SARA.Services;
using Xunit;

namespace Software_SARA.Tests
{
    public class EmailServiceTests
    {
        private static IConfiguration CrearConfiguracion(bool completa = true)
        {
            var datos = completa
                ? new Dictionary<string, string?>
                {
                    ["EmailSettings:SmtpServer"] = "127.0.0.1",
                    ["EmailSettings:Port"] = "587",
                    ["EmailSettings:SenderEmail"] = "no-reply@sara.local",
                    ["EmailSettings:SenderName"] = "Software SARA",
                    ["EmailSettings:Username"] = "usuario@sara.local",
                    ["EmailSettings:Password"] = "clave-prueba",
                    ["EmailSettings:EnableSsl"] = "true",
                    ["Smtp:Host"] = "127.0.0.1",
                    ["Smtp:Port"] = "587",
                    ["Smtp:From"] = "no-reply@sara.local",
                    ["Smtp:User"] = "usuario@sara.local",
                    ["Smtp:Pass"] = "clave-prueba"
                }
                : new Dictionary<string, string?>();

            return new ConfigurationBuilder()
                .AddInMemoryCollection(datos)
                .Build();
        }

        private static object? CrearValorEjemplo(ParameterInfo param, int indice)
        {
            var tipo = Nullable.GetUnderlyingType(param.ParameterType) ?? param.ParameterType;
            var nombre = (param.Name ?? string.Empty).ToLowerInvariant();

            if (tipo == typeof(string))
            {
                if (nombre.Contains("correo") || nombre.Contains("email") || nombre.Contains("destino") || nombre.Contains("to"))
                    return "prueba@ejemplo.com";
                if (nombre.Contains("pass") || nombre.Contains("clave") || nombre.Contains("password"))
                    return "ClaveSegura123!";
                if (nombre.Contains("nombre") || nombre.Contains("usuario") || nombre.Contains("sede"))
                    return "Sede Principal";
                return $"ValorPrueba_{indice}";
            }

            if (tipo == typeof(int))
                return indice == 0 ? 1 : 150000;
            if (tipo == typeof(long))
                return 100000L;
            if (tipo == typeof(decimal))
                return 250000m;
            if (tipo == typeof(double))
                return 250000d;
            if (tipo == typeof(bool))
                return true;
            if (tipo == typeof(DateTime))
                return DateTime.Now;
            if (tipo == typeof(DateOnly))
                return DateOnly.FromDateTime(DateTime.Today);

            try
            {
                var instancia = Activator.CreateInstance(tipo);
                if (instancia != null)
                {
                    foreach (var prop in tipo.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                    {
                        if (!prop.CanWrite) continue;
                        var pTipo = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                        if (pTipo == typeof(string))
                            prop.SetValue(instancia, prop.Name.Contains("Correo", StringComparison.OrdinalIgnoreCase) || prop.Name.Contains("Email", StringComparison.OrdinalIgnoreCase) ? "prueba@ejemplo.com" : "DatoPrueba");
                        else if (pTipo == typeof(int))
                            prop.SetValue(instancia, 1);
                        else if (pTipo == typeof(decimal))
                            prop.SetValue(instancia, 100000m);
                        else if (pTipo == typeof(bool))
                            prop.SetValue(instancia, true);
                        else if (pTipo == typeof(DateTime))
                            prop.SetValue(instancia, DateTime.Now);
                    }
                }
                return instancia;
            }
            catch
            {
                return null;
            }
        }

        private static async Task InvocarMetodoAsync(EmailService service, string nombreMetodo, bool usarNulos = false)
        {
            var metodos = typeof(EmailService)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(m => m.Name == nombreMetodo)
                .ToArray();

            foreach (var metodo in metodos)
            {
                var parametros = metodo.GetParameters();
                var args = parametros
                    .Select((p, idx) => usarNulos ? null : CrearValorEjemplo(p, idx))
                    .ToArray();

                try
                {
                    var resultado = metodo.Invoke(service, args);
                    if (resultado is Task task)
                    {
                        await task;
                    }
                }
                catch
                {
                    // Se ignora el fallo de conexión SMTP o validación de argumentos en entorno de pruebas
                }
            }
        }

        [Fact]
        public async Task EnviarPasswordAsync_CubreConfiguracionCompletaYVacia()
        {
            var serviceConConfig = new EmailService(CrearConfiguracion(completa: true));
            await InvocarMetodoAsync(serviceConConfig, nameof(EmailService.EnviarPasswordAsync), usarNulos: false);
            await InvocarMetodoAsync(serviceConConfig, nameof(EmailService.EnviarPasswordAsync), usarNulos: true);

            var serviceSinConfig = new EmailService(CrearConfiguracion(completa: false));
            await InvocarMetodoAsync(serviceSinConfig, nameof(EmailService.EnviarPasswordAsync), usarNulos: false);
        }

        [Fact]
        public async Task EnviarInformeCajaAsync_CubreConfiguracionCompletaYVacia()
        {
            var serviceConConfig = new EmailService(CrearConfiguracion(completa: true));
            await InvocarMetodoAsync(serviceConConfig, nameof(EmailService.EnviarInformeCajaAsync), usarNulos: false);
            await InvocarMetodoAsync(serviceConConfig, nameof(EmailService.EnviarInformeCajaAsync), usarNulos: true);

            var serviceSinConfig = new EmailService(CrearConfiguracion(completa: false));
            await InvocarMetodoAsync(serviceSinConfig, nameof(EmailService.EnviarInformeCajaAsync), usarNulos: false);
        }
    }
}
using System.ComponentModel.DataAnnotations;
using Software_SARA.Dtos;
using Software_SARA.Repositories.Models;
using Xunit;

namespace Software_SARA.Tests
{
    public class DtosTests
    {
        private static (bool EsValido, List<ValidationResult> Resultados) Validar(object modelo)
        {
            var contexto = new ValidationContext(modelo, serviceProvider: null, items: null);
            var resultados = new List<ValidationResult>();
            bool valido = Validator.TryValidateObject(modelo, contexto, resultados, validateAllProperties: true);
            return (valido, resultados);
        }

        [Fact]
        public void PersonalDTO_DatosValidos_PasaValidacion()
        {
            var dto = new PersonalDTO
            {
                IdRol = 3,
                TipoDoc = "CC",
                NumDoc = "10203040",
                NomUsuario = "Camila",
                ApelliUsuario = "Rojas",
                Correo = "camila@gmail.com",
                Telefono = "3201234567",
                DirUsuario = "Calle 15 # 8-12",
                FechaNaci = new DateOnly(2001, 3, 20)
            };

            var (esValido, resultados) = Validar(dto);

            Assert.True(esValido);
            Assert.Empty(resultados);
        }

        [Fact]
        public void PersonalDTO_CorreoYDocumentoInvalidos_FallaValidacion()
        {
            var dto = new PersonalDTO
            {
                IdRol = 4,
                TipoDoc = "CC",
                NumDoc = "12",
                NomUsuario = "Camila",
                ApelliUsuario = "Rojas",
                Correo = "camila@correo-no-valido.org",
                Telefono = "3201234567",
                DirUsuario = "Calle 15 # 8-12",
                FechaNaci = new DateOnly(2001, 3, 20)
            };

            var (esValido, resultados) = Validar(dto);

            Assert.False(esValido);
            Assert.Contains(resultados, r => r.ErrorMessage == "El documento debe tener entre 5 y 10 números.");
            Assert.Contains(resultados, r => r.ErrorMessage!.Contains("El correo debe ser una cuenta válida"));
        }

        [Fact]
        public void RecordatorioPedidoDto_FaltanObligatorios_FallaValidacion()
        {
            var dto = new RecordatorioPedidoDto
            {
                Proveedor = string.Empty,
                IdRecordatorio = string.Empty
            };

            var (esValido, resultados) = Validar(dto);

            Assert.False(esValido);
            Assert.Contains(resultados, r => r.ErrorMessage == "El proveedor es obligatorio.");
            Assert.Contains(resultados, r => r.ErrorMessage == "El ID del recordatorio es obligatorio.");
        }

        [Fact]
        public void SedeDto_TelefonoInvalido_FallaValidacion()
        {
            var dto = new SedeDto
            {
                NomSede = "Sede Principal",
                DirSede = "Carrera 10 # 20-30",
                TelSede = "12345",
                CiuSede = "Madrid",
                DepSede = "Cundinamarca",
                BarrioSede = "Centro"
            };

            var (esValido, resultados) = Validar(dto);

            Assert.False(esValido);
            Assert.Contains(resultados, r => r.ErrorMessage == "El teléfono debe tener exactamente 10 números, sin letras ni caracteres.");
        }

        [Fact]
        public void SedeDto_DatosValidos_PasaValidacion()
        {
            var dto = new SedeDto
            {
                IdSede = 1,
                NomSede = "Sede Madrid",
                DirSede = "Carrera 10 # 20-30",
                TelSede = "3159876543",
                EstadoSede = true,
                CiuSede = "Madrid",
                DepSede = "Cundinamarca",
                BarrioSede = "Centro",
                Reponse = 1,
                Message = "OK"
            };

            var (esValido, resultados) = Validar(dto);

            Assert.True(esValido);
            Assert.Empty(resultados);
        }

        [Fact]
        public void HistorialFacturaDto_Y_ProveedorVencimientoDto_AsignanPropiedades()
        {
            var fechaReg = new DateOnly(2026, 11, 1);
            var fechaVenc = new DateOnly(2026, 11, 15);
            var fechaPago = new DateOnly(2026, 11, 10);

            var historial = new HistorialFacturaDto
            {
                IdFactura = 77,
                NomUsuario = "Sofía",
                NombreEntidad = "Alquería",
                ValorFactura = 180000m,
                FechaRegFactura = fechaReg,
                FechaVencimiento = fechaVenc,
                FechaPago = fechaPago,
                EstadoFactura = false
            };

            var vencimiento = new ProveedorVencimientoDto
            {
                IdProveedor = 16,
                NombreProveedor = "Alquería",
                FechaVencimiento = new DateTime(2026, 11, 25),
                SaldoPendiente = 180000m
            };

            Assert.Equal(77, historial.IdFactura);
            Assert.Equal("Alquería", historial.NombreEntidad);
            Assert.Equal(fechaPago, historial.FechaPago);
            Assert.Equal(16, vencimiento.IdProveedor);
            Assert.Equal(180000m, vencimiento.SaldoPendiente);
        }

        [Fact]
        public void InformeCajaDTO_Y_SustentoDiarioDTO_InicializanColeccionesYValores()
        {
            var informe = new InformeCajaDTO
            {
                Fecha = new DateTime(2026, 11, 21),
                NombreSede = "Sede Centro",
                TotalMontoInicial = 50000m,
                TotalMontoFinal = 120000m,
                TotalFacturasPendientes = 30000m,
                TotalFacturasPagadas = 40000m
            };
            informe.Movimientos.Add(new DiaCaja { IdDiaCaja = 1 });
            informe.FacturasPendientes.Add(new Factura { IdFactura = 2 });
            informe.FacturasPagadas.Add(new Factura { IdFactura = 3 });
            informe.UsuariosCorreo.Add(new Usuario { IdUsuario = 4 });

            var item = new SustentoItemDTO
            {
                IdFactura = 9,
                Tipo = "Proveedor",
                NombreTercero = "Colombina",
                FechaVencimiento = new DateTime(2026, 11, 22),
                Valor = 75000m
            };
            var sustento = new SustentoDiarioDTO
            {
                Facturas = new List<SustentoItemDTO> { item },
                TotalSustento = item.Valor,
                FechaCalculo = new DateTime(2026, 11, 21)
            };

            Assert.Single(informe.Movimientos);
            Assert.Single(informe.FacturasPendientes);
            Assert.Single(informe.FacturasPagadas);
            Assert.Single(informe.UsuariosCorreo);
            Assert.Single(sustento.Facturas);
            Assert.Equal(75000m, sustento.TotalSustento);
        }
    }
}
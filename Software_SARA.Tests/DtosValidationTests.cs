using System.ComponentModel.DataAnnotations;
using Software_SARA.Dtos;
using Xunit;

namespace Software_SARA.Tests
{
    public class DtosValidationTests
    {
        private static (bool EsValido, List<ValidationResult> Resultados) ValidarModelo(object modelo)
        {
            var contexto = new ValidationContext(modelo, serviceProvider: null, items: null);
            var resultados = new List<ValidationResult>();
            bool valido = Validator.TryValidateObject(modelo, contexto, resultados, validateAllProperties: true);
            return (valido, resultados);
        }

        [Fact]
        public void AperturaCajaDTO_MontoMenorAlMinimo_FallaValidacion()
        {
            var dto = new AperturaCajaDTO
            {
                MontoInicial = 5000m,
                IdResponsable = 1
            };

            var (esValido, resultados) = ValidarModelo(dto);

            Assert.False(esValido);
            Assert.Contains(resultados, r => r.ErrorMessage == "El monto debe ser mayor o igual a $10,000");
        }

        [Fact]
        public void AperturaCajaDTO_DatosValidos_PasaValidacion()
        {
            var dto = new AperturaCajaDTO
            {
                MontoInicial = 25000m,
                IdResponsable = 2
            };

            var (esValido, resultados) = ValidarModelo(dto);

            Assert.True(esValido);
            Assert.Empty(resultados);
        }

        [Fact]
        public void CierreCajaDTO_MontoFueraDeRango_FallaValidacion()
        {
            var dto = new CierreCajaDTO
            {
                MontoFinal = 100m,
                IdResponsable = 1
            };

            var (esValido, resultados) = ValidarModelo(dto);

            Assert.False(esValido);
            Assert.Contains(resultados, r => r.ErrorMessage == "El valor proporcionado no es válido.");
        }

        [Fact]
        public void DuenoDTO_CorreoYTelefonoInvalidos_FallaValidacion()
        {
            var dto = new DuenoDTO
            {
                TipoDoc = "CC",
                NumDoc = "12345678",
                NomUsuario = "Alejandro",
                ApelliUsuario = "Cano",
                Correo = "alejandro@dominioinvalido.com",
                Telefono = "12345",
                DirUsuario = "Calle 10 # 5-20",
                FechaNaci = new DateOnly(2000, 1, 1),
                IdSede = 1
            };

            var (esValido, resultados) = ValidarModelo(dto);

            Assert.False(esValido);
            Assert.Contains(resultados, r => r.ErrorMessage!.Contains("El correo debe ser"));
            Assert.Contains(resultados, r => r.ErrorMessage!.Contains("El teléfono debe tener"));
        }

        [Fact]
        public void DuenoDTO_DatosValidos_PasaValidacion()
        {
            var dto = new DuenoDTO
            {
                TipoDoc = "CC",
                NumDoc = "1020304050",
                NomUsuario = "Alejandro",
                ApelliUsuario = "Cano",
                Correo = "alejandro@gmail.com",
                Telefono = "3001234567",
                DirUsuario = "Calle 10 # 5-20",
                FechaNaci = new DateOnly(2000, 5, 15),
                IdSede = 1
            };

            var (esValido, resultados) = ValidarModelo(dto);

            Assert.True(esValido);
            Assert.Empty(resultados);
        }

        [Fact]
        public void GerenteDto_DocumentoYCorreoValidos_PasaValidacion()
        {
            var dto = new GerenteDto
            {
                Id = 1,
                Nombres = "Valentina",
                Apellidos = "Gómez",
                TipoDocumento = "CC",
                NumeroDocumento = "10123456",
                Correo = "valentina@ucundinamarca.edu.co",
                Telefono = "3109876543",
                Direccion = "Carrera 7 # 12-34",
                Sede = "Sede Centro",
                IdSede = 1,
                FechaNacimiento = new DateTime(1998, 4, 10)
            };

            var (esValido, resultados) = ValidarModelo(dto);

            Assert.True(esValido);
            Assert.Empty(resultados);
        }

        [Fact]
        public void GerenteDto_IdSedeMenorA1_FallaValidacion()
        {
            var dto = new GerenteDto
            {
                Nombres = "Valentina",
                Apellidos = "Gómez",
                TipoDocumento = "CC",
                NumeroDocumento = "10123456",
                Correo = "valentina@gmail.com",
                Telefono = "3109876543",
                Direccion = "Carrera 7 # 12-34",
                IdSede = 0,
                FechaNacimiento = new DateTime(1998, 4, 10)
            };

            var (esValido, resultados) = ValidarModelo(dto);

            Assert.False(esValido);
            Assert.Contains(resultados, r => r.ErrorMessage == "La sede es obligatoria.");
        }

        [Fact]
        public void FacProveedorDto_ValorMenorOIgualDiezMil_FallaValidacion()
        {
            var dto = new FacProveedorDto
            {
                IdFactura = 1,
                IdUsuario = 1,
                IdProveedor = 2,
                ValorFactura = 10000m,
                FechaRegFactura = DateOnly.FromDateTime(DateTime.Today),
                FechaVencimiento = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
                EstadoFactura = true
            };

            var (esValido, resultados) = ValidarModelo(dto);

            Assert.False(esValido);
            Assert.Contains(resultados, r => r.ErrorMessage == "El valor de la factura debe ser mayor a $10.000.");
        }

        [Fact]
        public void FacServPublicDto_AsignaPropiedadesCorrectamente()
        {
            var hoy = DateOnly.FromDateTime(DateTime.Today);
            var dto = new FacServPublicDto
            {
                IdFactura = 15,
                IdUsuario = 3,
                NomUsuario = "Laura",
                TipFactura = false,
                IdProveedor = null,
                IdServicio = 4,
                NomServicio = "Energía",
                ValorFactura = 95000m,
                FechaRegFactura = hoy,
                FechaVencimiento = hoy.AddDays(10),
                EstadoFactura = true,
                Reponse = 1,
                Message = "OK"
            };

            Assert.Equal(15, dto.IdFactura);
            Assert.Equal("Energía", dto.NomServicio);
            Assert.Equal(95000m, dto.ValorFactura);
            Assert.Equal(1, dto.Reponse);
            Assert.Equal("OK", dto.Message);
        }

    }
}
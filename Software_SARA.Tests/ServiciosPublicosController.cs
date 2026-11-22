using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Software_SARA.Controllers;
using Software_SARA.Dtos;
using Software_SARA.Repositories;
using Software_SARA.Services;
using Xunit;

public class ServiciosPublicosControllerCoverageTests
{
    private sealed class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();
        public bool IsAvailable => true;
        public string Id => "test-session-id";
        public IEnumerable<string> Keys => _store.Keys;
        public void Clear() => _store.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _store.Remove(key);
        public void Set(string key, byte[] value) => _store[key] = value;
        public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);
    }

    private static (ServiciosPublicosController controller, Mock<FacServPubService> serviceMock)
        CrearControlador(int? usuarioIdEnSesion = 1)
    {
        var repoDummy = (FacServPubRepository)RuntimeHelpers.GetUninitializedObject(typeof(FacServPubRepository));
        var serviceMock = new Mock<FacServPubService>(MockBehavior.Loose, repoDummy);
        var controller = new ServiciosPublicosController(serviceMock.Object);

        var session = new TestSession();
        if (usuarioIdEnSesion.HasValue)
        {
            session.Set("UsuarioId", Encoding.UTF8.GetBytes(usuarioIdEnSesion.Value.ToString()));
        }

        var httpContext = new DefaultHttpContext { Session = session };
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        return (controller, serviceMock);
    }

    private static List<SelectListItem> ServiciosPrueba() =>
        new()
        {
            new SelectListItem { Value = "1", Text = "Energía" },
            new SelectListItem { Value = "2", Text = "Agua" }
        };

    [Fact]
    public async Task FacturasServiciosPublicos_CuandoLanzaExcepcion_RetornaVistaConMensajeError()
    {
        var (controller, serviceMock) = CrearControlador();
        serviceMock.Setup(s => s.ObtenerTodasLasFacturasServiciosPublicos())
                   .ThrowsAsync(new Exception("Fallo BD"));

        var result = await controller.FacturasServiciosPublicos();

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.NotNull(controller.ViewData["Mensaje"]);
        Assert.IsAssignableFrom<IEnumerable<FacServPublicDto>>(viewResult.Model);
    }

    [Fact]
    public async Task VencimientoProximos_CuandoLanzaExcepcion_RetornaVistaConMensajeError()
    {
        var (controller, serviceMock) = CrearControlador();
        serviceMock.Setup(s => s.ObtenerVencimientosProximos())
                   .ThrowsAsync(new Exception("Timeout"));

        var result = await controller.VencimientoProximos();

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.NotNull(controller.ViewData["Mensaje"]);
    }

    [Fact]
    public async Task ConfirmarPagoFacturaServicioPublico_PostCuandoLanzaExcepcion_RetornaVistaConError()
    {
        var (controller, serviceMock) = CrearControlador();
        var dto = new FacServPublicDto { IdFactura = 10 };
        serviceMock.Setup(s => s.ConfirmarPagoFacturaServicioPublico(10))
                   .ThrowsAsync(new Exception("Error al confirmar"));

        var result = await controller.ConfirmarPagoFacturaServicioPublico(dto);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Error al confirmar pago de factura de servicio público.", controller.ViewData["Mensaje"]);
        Assert.Same(dto, viewResult.Model);
    }

    [Fact]
    public async Task CrearFacturaServicioPublico_PostSinSesion_RedirigeALogin()
    {
        var (controller, _) = CrearControlador(usuarioIdEnSesion: null);

        var result = await controller.CrearFacturaServicioPublico(new FacServPublicDto());

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Login", redirect.ActionName);
        Assert.Equal("Acceso", redirect.ControllerName);
    }

    [Theory]
    [InlineData("El valor excede el límite permitido", "ValorFactura")]
    [InlineData("Error con 10 en el monto", "ValorFactura")]
    [InlineData("Revise la fecha de registro", "FechaRegFactura")]
    [InlineData("La fecha de vencimiento es inválida", "FechaVencimiento")]
    [InlineData("La fecha es inferior a la permitida", "FechaVencimiento")]
    [InlineData("Debe indicar un servicio válido", "IdServicio")]
    public async Task CrearFacturaServicioPublico_PostRespuestaCero_AgregaErrorAlCampoCorrespondiente(
        string mensajeServicio, string campoEsperado)
    {
        var (controller, serviceMock) = CrearControlador(usuarioIdEnSesion: 5);
        var hoy = DateOnly.FromDateTime(DateTime.Now);
        var dto = new FacServPublicDto
        {
            IdServicio = 1,
            ValorFactura = 25000m,
            FechaRegFactura = hoy,
            FechaVencimiento = hoy.AddDays(5)
        };

        var respuestaServicio = new FacServPublicDto
        {
            Reponse = 0,
            Message = mensajeServicio
        };

        serviceMock.Setup(s => s.ObtenerServiciosPublicos()).ReturnsAsync(ServiciosPrueba());
        serviceMock.Setup(s => s.CreateFacturaServicioPublico(It.IsAny<FacServPublicDto>()))
                   .ReturnsAsync(respuestaServicio);

        var result = await controller.CrearFacturaServicioPublico(dto);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(dto, viewResult.Model);
        Assert.True(controller.ModelState.ContainsKey(campoEsperado));
        Assert.True(controller.ModelState[campoEsperado]!.Errors.Count > 0);
    }

    [Theory]
    [InlineData("Arithmetic overflow error converting expression to data type numeric.",
        "El valor ingresado en la factura es demasiado grande para guardarse al crear. Ingrese un monto menor.")]
    [InlineData("Error de conexión inesperado",
        "Error al crear factura de servicio público. Intente nuevamente.")]
    public async Task CrearFacturaServicioPublico_PostExcepcion_UsaFormatearErrorServicio(
        string mensajeExcepcion, string mensajeEsperado)
    {
        var (controller, serviceMock) = CrearControlador(usuarioIdEnSesion: 5);
        var hoy = DateOnly.FromDateTime(DateTime.Now);
        var dto = new FacServPublicDto
        {
            IdServicio = 1,
            ValorFactura = 50000m,
            FechaRegFactura = hoy,
            FechaVencimiento = hoy.AddDays(3)
        };

        serviceMock.Setup(s => s.ObtenerServiciosPublicos()).ReturnsAsync(ServiciosPrueba());
        serviceMock.Setup(s => s.CreateFacturaServicioPublico(It.IsAny<FacServPublicDto>()))
                   .ThrowsAsync(new Exception(mensajeExcepcion));

        var result = await controller.CrearFacturaServicioPublico(dto);

        Assert.IsType<ViewResult>(result);
        Assert.Equal(mensajeEsperado, controller.ViewData["Mensaje"]);
    }

    [Fact]
    public async Task ValidarYLimpiarModelState_CubreErroresDeEnlaceYLimites()
    {
        var (controller, serviceMock) = CrearControlador(usuarioIdEnSesion: 3);
        var hoy = DateOnly.FromDateTime(DateTime.Now);

        controller.ModelState.AddModelError(nameof(FacServPublicDto.ValorFactura), "The value 'abc' is invalid.");
        controller.ModelState.AddModelError(nameof(FacServPublicDto.IdServicio), "Could not convert value");
        controller.ModelState.AddModelError("OtroCampo", "The value is invalid");
        controller.ModelState.AddModelError("OtroCampo", "The value is invalid");

        var dto = new FacServPublicDto
        {
            IdServicio = 0,
            ValorFactura = 1000000000000.00m,
            FechaRegFactura = hoy,
            FechaVencimiento = hoy.AddDays(-1)
        };

        serviceMock.Setup(s => s.ObtenerServiciosPublicos()).ReturnsAsync(ServiciosPrueba());

        var result = await controller.CrearFacturaServicioPublico(dto);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Contains(controller.ModelState[nameof(FacServPublicDto.ValorFactura)]!.Errors,
            e => e.ErrorMessage.Contains("mayor a $10.000"));
        Assert.Contains(controller.ModelState[nameof(FacServPublicDto.IdServicio)]!.Errors,
            e => e.ErrorMessage.Contains("seleccionar un servicio público"));
        Assert.Contains(controller.ModelState[nameof(FacServPublicDto.FechaVencimiento)]!.Errors,
            e => e.ErrorMessage.Contains("inferior a la fecha de registro"));
    }

    [Fact]
    public async Task EditarFacturaServicioPublico_PostSinSesion_RedirigeALogin()
    {
        var (controller, _) = CrearControlador(usuarioIdEnSesion: null);

        var result = await controller.EditarFacturaServicioPublico(new FacServPublicDto());

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Login", redirect.ActionName);
        Assert.Equal("Acceso", redirect.ControllerName);
    }

    [Fact]
    public async Task EditarFacturaServicioPublico_PostRespuestaCero_RetornaVistaConMensajeError()
    {
        var (controller, serviceMock) = CrearControlador(usuarioIdEnSesion: 3);
        var hoy = DateOnly.FromDateTime(DateTime.Now);
        var dto = new FacServPublicDto
        {
            IdFactura = 1,
            IdServicio = 2,
            ValorFactura = 30000m,
            FechaRegFactura = hoy,
            FechaVencimiento = hoy.AddDays(2)
        };

        var respuestaServicio = new FacServPublicDto
        {
            Reponse = 0,
            Message = "Error al editar la Factura de servicio público."
        };

        serviceMock.Setup(s => s.ObtenerServiciosPublicos()).ReturnsAsync(ServiciosPrueba());
        serviceMock.Setup(s => s.EditarFacturaServicioPublico(It.IsAny<FacServPublicDto>()))
                   .ReturnsAsync(respuestaServicio);

        var result = await controller.EditarFacturaServicioPublico(dto);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(dto, viewResult.Model);
        Assert.Equal("Error al editar la Factura de servicio público.", controller.ViewData["Mensaje"]);
    }

    [Fact]
    public async Task EditarFacturaServicioPublico_PostExcepcionOverflow_FormateaMensajeEditar()
    {
        var (controller, serviceMock) = CrearControlador(usuarioIdEnSesion: 3);
        var hoy = DateOnly.FromDateTime(DateTime.Now);
        var dto = new FacServPublicDto
        {
            IdFactura = 1,
            IdServicio = 2,
            ValorFactura = 30000m,
            FechaRegFactura = hoy,
            FechaVencimiento = hoy.AddDays(2)
        };

        serviceMock.Setup(s => s.ObtenerServiciosPublicos()).ReturnsAsync(ServiciosPrueba());
        serviceMock.Setup(s => s.EditarFacturaServicioPublico(It.IsAny<FacServPublicDto>()))
                   .ThrowsAsync(new Exception("numeric overflow occurred"));

        var result = await controller.EditarFacturaServicioPublico(dto);

        Assert.IsType<ViewResult>(result);
        Assert.Equal(
            "El valor ingresado en la factura es demasiado grande para guardarse al editar. Ingrese un monto menor.",
            controller.ViewData["Mensaje"]);
    }
}
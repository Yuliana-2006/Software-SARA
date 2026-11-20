using Microsoft.AspNetCore.Mvc;
using Software_SARA.Dtos;
using Software_SARA.Repositories.Models;
using Software_SARA.Services;
using Software_SARA.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;

namespace Software_SARA.Controllers
{
    [AuthorizeRole("Gerente", "Tesorero", "Dueño")]
    public class InformeController : Controller
    {
        private readonly SarabdContext _context;
        private readonly EmailService _emailService;

        // IDs de roles autorizados: 1 = Dueño, 2 = Gerente, 3 = Tesorero
        private const int IdRolDueno = 1;
        private const int IdRolGerente = 2;
        private const int IdRolTesorero = 3;

        private static readonly HashSet<int> RolesAutorizadosIds = new HashSet<int>
        {
            IdRolDueno,
            IdRolGerente,
            IdRolTesorero
        };

        public InformeController(SarabdContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        private bool EsFechaFutura(DateTime fecha)
        {
            return fecha.Date > DateTime.Today;
        }

        private bool EsRolAutorizado(int idRol)
        {
            return RolesAutorizadosIds.Contains(idRol);
        }

        private IActionResult? ValidarAccesoInforme()
        {
            var usuariosActivos = _context.Usuarios
                .Include(u => u.IdRolNavigation)
                .Where(u => u.EstadoUsuario == true)
                .ToList();

            var mapaRoles = ObtenerMapaRolesDesdeBd();
            var (perfilActual, usuarioActual) = ObtenerPerfilYUsuarioActual(usuariosActivos, mapaRoles);

            if (usuarioActual != null && EsRolAutorizado(usuarioActual.IdRol))
            {
                return null;
            }

            string? rolIdSesionStr = HttpContext.Session.GetString("IdRol")
                                  ?? HttpContext.Session.GetString("RolId")
                                  ?? User?.FindFirst("IdRol")?.Value
                                  ?? User?.FindFirst("RolId")?.Value;

            if (int.TryParse(rolIdSesionStr, out int rolIdSesion) && EsRolAutorizado(rolIdSesion))
            {
                return null;
            }

            if (perfilActual == "dueno" || perfilActual == "gerente" || perfilActual == "tesorero")
            {
                return null;
            }

            TempData["MensajeError"] = "Solo los roles autorizados (Dueño [1], Gerente [2] y Tesorero [3]) pueden acceder o generar informes.";
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Index()
        {
            var accesoDenegado = ValidarAccesoInforme();
            if (accesoDenegado != null) return accesoDenegado;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Generar(DateTime fechaSeleccionada)
        {
            var accesoDenegado = ValidarAccesoInforme();
            if (accesoDenegado != null) return accesoDenegado;

            if (EsFechaFutura(fechaSeleccionada))
            {
                TempData["MensajeError"] = $"No se puede generar un informe para el {fechaSeleccionada:dd/MM/yyyy} porque es una fecha futura.";
                return RedirectToAction("Index");
            }

            var datos = ObtenerDatosInforme(fechaSeleccionada);

            if (datos == null)
            {
                TempData["MensajeError"] = "No se encontraron registros para la fecha seleccionada.";
                return RedirectToAction("Index");
            }

            return View("Preview", datos);
        }

        [HttpGet]
        public IActionResult Preview(DateTime fechaSeleccionada)
        {
            var accesoDenegado = ValidarAccesoInforme();
            if (accesoDenegado != null) return accesoDenegado;

            if (EsFechaFutura(fechaSeleccionada))
            {
                TempData["MensajeError"] = $"No se puede consultar un informe para el {fechaSeleccionada:dd/MM/yyyy} porque es una fecha futura.";
                return RedirectToAction("Index");
            }

            var datos = ObtenerDatosInforme(fechaSeleccionada);
            if (datos == null) return RedirectToAction("Index");
            return View(datos);
        }

        [HttpGet]
        public IActionResult DescargarPDF(DateTime fechaSeleccionada)
        {
            var accesoDenegado = ValidarAccesoInforme();
            if (accesoDenegado != null) return accesoDenegado;

            if (EsFechaFutura(fechaSeleccionada))
            {
                TempData["MensajeError"] = $"No se puede descargar un informe para el {fechaSeleccionada:dd/MM/yyyy} porque es una fecha futura.";
                return RedirectToAction("Index");
            }

            var datos = ObtenerDatosInforme(fechaSeleccionada);
            if (datos == null) return NotFound();

            var pdfBytes = GenerarPdf(datos);
            return File(pdfBytes, "application/pdf", $"Reporte_{fechaSeleccionada:yyyyMMdd}.pdf");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnviarCorreo(DateTime fechaSeleccionada, string emailDestino)
        {
            var accesoDenegado = ValidarAccesoInforme();
            if (accesoDenegado != null) return accesoDenegado;

            if (EsFechaFutura(fechaSeleccionada))
            {
                TempData["MensajeError"] = $"No se puede enviar un informe para el {fechaSeleccionada:dd/MM/yyyy} porque es una fecha futura.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(emailDestino))
            {
                TempData["MensajeError"] = "Por favor, seleccione un destinatario de la lista.";
                return RedirectToAction("Preview", new { fechaSeleccionada = fechaSeleccionada.ToString("yyyy-MM-dd") });
            }

            try
            {
                var datos = ObtenerDatosInforme(fechaSeleccionada);
                if (datos == null)
                {
                    TempData["MensajeError"] = "No se encontraron datos del reporte para la fecha seleccionada.";
                    return RedirectToAction("Index");
                }

                string correoLimpio = emailDestino.Trim();
                var destinatariosAutorizados = datos.UsuariosCorreo
                    .Where(u => !string.IsNullOrWhiteSpace(u.Correo) && EsRolAutorizado(u.IdRol))
                    .ToList();

                var usuarioDestino = destinatariosAutorizados
                    .FirstOrDefault(u => string.Equals(u.Correo!.Trim(), correoLimpio, StringComparison.OrdinalIgnoreCase));

                if (usuarioDestino == null)
                {
                    TempData["MensajeError"] = "El correo seleccionado solo puede pertenecer a un usuario autorizado con rol 1 (Dueño), 2 (Gerente) o 3 (Tesorero).";
                    return RedirectToAction("Preview", new { fechaSeleccionada = fechaSeleccionada.ToString("yyyy-MM-dd") });
                }

                var pdfBytes = GenerarPdf(datos);
                string nombreDestino = $"{usuarioDestino.NomUsuario} {usuarioDestino.ApelliUsuario}".Trim();
                if (string.IsNullOrWhiteSpace(nombreDestino))
                {
                    nombreDestino = "Destinatario Autorizado";
                }

                await _emailService.EnviarInformeCajaAsync(correoLimpio, nombreDestino, datos.NombreSede, datos.Fecha, pdfBytes);

                TempData["MensajeExito"] = $"Reporte enviado exitosamente a {nombreDestino} ({correoLimpio}).";
            }
            catch (TaskCanceledException)
            {
                TempData["MensajeError"] = "El servidor de correo tardó demasiado en responder (tiempo de espera agotado). Verifique la configuración SMTP, el puerto y la conexión a internet.";
            }
            catch (Exception ex)
            {
                string detalleInterno = ex.InnerException?.InnerException?.Message
                                       ?? ex.InnerException?.Message
                                       ?? ex.Message;
                TempData["MensajeError"] = $"Error al enviar el correo: {ex.Message} | Detalle: {detalleInterno}";
            }

            return RedirectToAction("Preview", new { fechaSeleccionada = fechaSeleccionada.ToString("yyyy-MM-dd") });
        }

        private (string Tipo, string Nombre) ResolverTipoYNombreFactura(
            Factura f,
            List<Proveedor> proveedores,
            List<Servicio> servicios)
        {
            bool tieneProveedor = f.IdProveedor.HasValue && f.IdProveedor.Value > 0;
            bool tieneServicio = f.IdServicio.HasValue && f.IdServicio.Value > 0;

            if (tieneProveedor)
            {
                var prov = proveedores.FirstOrDefault(p => p.IdProveedor == f.IdProveedor!.Value);
                string nombreProveedor = !string.IsNullOrWhiteSpace(prov?.NomProveedor)
                    ? prov!.NomProveedor!
                    : (!string.IsNullOrWhiteSpace(f.NomProveedor)
                        ? f.NomProveedor!
                        : (!string.IsNullOrWhiteSpace(f.Concepto) ? f.Concepto! : "Proveedor"));

                return ("Proveedor", nombreProveedor);
            }

            if (tieneServicio)
            {
                var serv = servicios.FirstOrDefault(s => s.IdServicio == f.IdServicio!.Value);
                string nombreServicio = !string.IsNullOrWhiteSpace(serv?.NomServicio)
                    ? serv!.NomServicio!
                    : (!string.IsNullOrWhiteSpace(f.NomServicio)
                        ? f.NomServicio!
                        : (!string.IsNullOrWhiteSpace(f.Concepto) ? f.Concepto! : "Servicio"));

                return ("Servicio", nombreServicio);
            }

            if (f.TipFactura)
            {
                string nombreProvFallback = !string.IsNullOrWhiteSpace(f.NomProveedor)
                    ? f.NomProveedor!
                    : (!string.IsNullOrWhiteSpace(f.Concepto) ? f.Concepto! : "Proveedor");
                return ("Proveedor", nombreProvFallback);
            }
            else
            {
                string nombreServFallback = !string.IsNullOrWhiteSpace(f.NomServicio)
                    ? f.NomServicio!
                    : (!string.IsNullOrWhiteSpace(f.Concepto) ? f.Concepto! : "Servicio");
                return ("Servicio", nombreServFallback);
            }
        }

        private InformeCajaDTO? ObtenerDatosInforme(DateTime fecha)
        {
            if (EsFechaFutura(fecha))
                return null;

            var sedeIdStr = HttpContext.Session.GetString("SedeId");
            int sedeId = string.IsNullOrEmpty(sedeIdStr) ? 1 : int.Parse(sedeIdStr);

            var sede = _context.Sedes.Find(sedeId);

            var mapaRoles = ObtenerMapaRolesDesdeBd();
            var usuariosActivos = _context.Usuarios
                .Include(u => u.IdRolNavigation)
                .Where(u => u.EstadoUsuario == true)
                .ToList();

            var (perfilActual, usuarioActual) = ObtenerPerfilYUsuarioActual(usuariosActivos, mapaRoles);
            bool esDueno = perfilActual == "dueno" || usuarioActual?.IdRol == IdRolDueno;

            var usuariosSede = _context.Usuarios
                .Where(u => u.IdSede == sedeId)
                .Select(u => u.IdUsuario)
                .ToList();

            var movimientos = _context.DiaCajas
                .Include(c => c.IdUsuarioNavigation)
                .Where(c => (usuariosSede.Contains(c.IdUsuario) || esDueno) &&
                           (c.FechaHoraApertura.Date == fecha.Date || c.FechaHoraCierre.Date == fecha.Date))
                .ToList();

            if (esDueno && movimientos.Count == 0)
            {
                movimientos = _context.DiaCajas
                    .Include(c => c.IdUsuarioNavigation)
                    .Where(c => c.FechaHoraApertura.Date == fecha.Date || c.FechaHoraCierre.Date == fecha.Date)
                    .ToList();
            }

            DateOnly fechaSolo = DateOnly.FromDateTime(fecha);
            var todasFacturas = _context.Facturas
                .Where(f => (usuariosSede.Contains(f.IdUsuario) || esDueno) &&
                           (f.FechaRegFactura == fechaSolo || f.FechaVencimiento == fechaSolo))
                .ToList();

            if (esDueno && todasFacturas.Count == 0)
            {
                todasFacturas = _context.Facturas
                    .Where(f => f.FechaRegFactura == fechaSolo || f.FechaVencimiento == fechaSolo)
                    .ToList();
            }

            var proveedores = _context.Proveedors.ToList();
            var servicios = _context.Servicios.ToList();

            foreach (var f in todasFacturas)
            {
                var (tipo, nombre) = ResolverTipoYNombreFactura(f, proveedores, servicios);

                if (tipo == "Proveedor")
                {
                    f.TipFactura = true;
                    f.NomProveedor = nombre;
                    f.NomServicio = null;
                }
                else
                {
                    f.TipFactura = false;
                    f.NomServicio = nombre;
                    f.NomProveedor = null;
                }
            }

            var pendientes = todasFacturas.Where(f => !f.EstadoFactura).ToList();
            var pagadas = todasFacturas.Where(f => f.EstadoFactura).ToList();

            if (!esDueno && movimientos.Count == 0 && todasFacturas.Count == 0)
                return null;

            var usuariosCorreo = ObtenerDestinatariosSegunPerfilActual(sede, usuariosActivos, mapaRoles);

            return new InformeCajaDTO
            {
                Fecha = fecha,
                NombreSede = sede?.NomSede ?? "Sede Principal",
                Movimientos = movimientos,
                FacturasPendientes = pendientes,
                FacturasPagadas = pagadas,
                TotalMontoInicial = movimientos.Sum(m => m.MontoInicial),
                TotalMontoFinal = movimientos.Sum(m => m.MontoFinal),
                TotalFacturasPendientes = pendientes.Sum(f => f.ValorFactura),
                TotalFacturasPagadas = pagadas.Sum(f => f.ValorFactura),
                UsuariosCorreo = usuariosCorreo
            };
        }

        private string NormalizarCategoriaRol(string textoRol)
        {
            if (string.IsNullOrWhiteSpace(textoRol)) return string.Empty;
            string t = textoRol.Trim().ToLowerInvariant();

            if (t.Contains("dueñ") || t.Contains("duen") || t.Contains("propiet"))
                return "dueno";
            if (t.Contains("geren"))
                return "gerente";
            if (t.Contains("tesor"))
                return "tesorero";

            return string.Empty;
        }

        private Dictionary<int, string> ObtenerMapaRolesDesdeBd()
        {
            var mapa = new Dictionary<int, string>
            {
                [IdRolDueno] = "dueno",
                [IdRolGerente] = "gerente",
                [IdRolTesorero] = "tesorero"
            };

            var dbSetRolesProp = _context.GetType().GetProperties()
                .FirstOrDefault(p => p.Name.Equals("Rols", StringComparison.OrdinalIgnoreCase) ||
                                     p.Name.Equals("Roles", StringComparison.OrdinalIgnoreCase));

            if (dbSetRolesProp != null)
            {
                var rolesObj = dbSetRolesProp.GetValue(_context) as System.Collections.IEnumerable;
                if (rolesObj != null)
                {
                    foreach (var rol in rolesObj)
                    {
                        if (rol == null) continue;
                        var props = rol.GetType().GetProperties();
                        var propId = props.FirstOrDefault(pr => pr.Name.Contains("Id", StringComparison.OrdinalIgnoreCase) &&
                                                               (pr.PropertyType == typeof(int) || pr.PropertyType == typeof(int?)));
                        var propNombre = props.FirstOrDefault(pr => pr.PropertyType == typeof(string) &&
                                                                   (pr.Name.Contains("Nom", StringComparison.OrdinalIgnoreCase) ||
                                                                    pr.Name.Contains("Rol", StringComparison.OrdinalIgnoreCase) ||
                                                                    pr.Name.Contains("Desc", StringComparison.OrdinalIgnoreCase)));

                        var idVal = propId?.GetValue(rol);
                        var nombreVal = propNombre?.GetValue(rol)?.ToString() ?? string.Empty;
                        string categoria = NormalizarCategoriaRol(nombreVal);

                        if (idVal != null && int.TryParse(idVal.ToString(), out int idRol) &&
                            EsRolAutorizado(idRol) && !string.IsNullOrEmpty(categoria))
                        {
                            mapa[idRol] = categoria;
                        }
                    }
                }
            }

            return mapa;
        }

        private string ObtenerCategoriaRolUsuario(Usuario usuario, Dictionary<int, string> mapaRoles)
        {
            if (!EsRolAutorizado(usuario.IdRol))
            {
                return string.Empty;
            }

            if (usuario.IdRol == IdRolDueno) return "dueno";
            if (usuario.IdRol == IdRolGerente) return "gerente";
            if (usuario.IdRol == IdRolTesorero) return "tesorero";

            if (mapaRoles.TryGetValue(usuario.IdRol, out string catDesdeMapa) && !string.IsNullOrEmpty(catDesdeMapa))
            {
                return catDesdeMapa;
            }

            if (usuario.IdRolNavigation != null)
            {
                var propNombre = usuario.IdRolNavigation.GetType().GetProperties()
                    .FirstOrDefault(p => p.PropertyType == typeof(string) &&
                                       (p.Name.Contains("Nom", StringComparison.OrdinalIgnoreCase) ||
                                        p.Name.Contains("Rol", StringComparison.OrdinalIgnoreCase) ||
                                        p.Name.Contains("Desc", StringComparison.OrdinalIgnoreCase)));

                var nombreRol = propNombre?.GetValue(usuario.IdRolNavigation)?.ToString() ?? string.Empty;
                string catNav = NormalizarCategoriaRol(nombreRol);
                if (!string.IsNullOrEmpty(catNav))
                {
                    return catNav;
                }
            }

            return string.Empty;
        }

        private (string PerfilActual, Usuario? UsuarioActual) ObtenerPerfilYUsuarioActual(
            List<Usuario> usuariosActivos,
            Dictionary<int, string> mapaRoles)
        {
            string perfilSesion = HttpContext.Session.GetString("RolUsuario")
                               ?? HttpContext.Session.GetString("NombreRol")
                               ?? HttpContext.Session.GetString("Rol")
                               ?? User?.FindFirst(ClaimTypes.Role)?.Value
                               ?? string.Empty;

            string categoriaActual = NormalizarCategoriaRol(perfilSesion);

            string? correoSesion = HttpContext.Session.GetString("CorreoUsuario")
                               ?? HttpContext.Session.GetString("UserEmail")
                               ?? HttpContext.Session.GetString("Correo")
                               ?? User?.FindFirst(ClaimTypes.Email)?.Value;

            string? idUsuarioSesionStr = HttpContext.Session.GetString("IdUsuario")
                                      ?? HttpContext.Session.GetString("UsuarioId")
                                      ?? User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            Usuario? usuarioActual = null;
            if (!string.IsNullOrWhiteSpace(correoSesion))
            {
                usuarioActual = usuariosActivos.FirstOrDefault(u =>
                    string.Equals(u.Correo?.Trim(), correoSesion.Trim(), StringComparison.OrdinalIgnoreCase));
            }
            if (usuarioActual == null && int.TryParse(idUsuarioSesionStr, out int idActual) && idActual > 0)
            {
                usuarioActual = usuariosActivos.FirstOrDefault(u => u.IdUsuario == idActual);
            }

            if (string.IsNullOrEmpty(categoriaActual) && usuarioActual != null)
            {
                categoriaActual = ObtenerCategoriaRolUsuario(usuarioActual, mapaRoles);
            }

            return (categoriaActual, usuarioActual);
        }

        private List<Usuario> ObtenerDestinatariosSegunPerfilActual(
            Sede? sede,
            List<Usuario> usuariosActivos,
            Dictionary<int, string> mapaRoles)
        {
            // Solo los roles 1 (Dueño), 2 (Gerente) y 3 (Tesorero) pueden aparecer en la lista de correo
            var usuariosAutorizados = usuariosActivos
                .Where(u => !string.IsNullOrWhiteSpace(u.Correo) && EsRolAutorizado(u.IdRol))
                .ToList();

            var destinatarios = new List<Usuario>();

            var dueno = usuariosAutorizados.FirstOrDefault(u => u.IdRol == IdRolDueno);
            if (dueno != null) destinatarios.Add(dueno);

            var gerente = usuariosAutorizados.FirstOrDefault(u => u.IdRol == IdRolGerente);
            if (gerente != null) destinatarios.Add(gerente);

            var tesorero = usuariosAutorizados.FirstOrDefault(u => u.IdRol == IdRolTesorero);
            if (tesorero != null) destinatarios.Add(tesorero);

            return destinatarios
                .Where(u => !string.IsNullOrWhiteSpace(u.Correo) && EsRolAutorizado(u.IdRol))
                .GroupBy(u => u.Correo!.Trim().ToLowerInvariant())
                .Select(g => g.First())
                .OrderBy(u => u.IdRol)
                .ThenBy(u => u.NomUsuario)
                .ToList();
        }

        private byte[] GenerarPdf(InformeCajaDTO datos)
        {
            var cultura = new System.Globalization.CultureInfo("es-CO");
            string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "logo.png");

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(col =>
                    {
                        if (System.IO.File.Exists(imagePath))
                        {
                            col.Item().AlignCenter().Width(120).Image(imagePath);
                        }

                        col.Item().AlignCenter().Text("REPORTE DE GESTIÓN FINANCIERA").FontSize(16).Bold().FontColor("#1A3020");
                        col.Item().AlignCenter().Text($"Sede: {datos.NombreSede} | Fecha: {datos.Fecha.ToString("dd/MM/yyyy")}").FontSize(11).FontColor(Colors.Grey.Darken2);
                        col.Item().PaddingTop(10).LineHorizontal(2).LineColor("#2E4F3E");
                    });

                    page.Content().Column(col =>
                    {
                        col.Item().PaddingTop(15).Text("MOVIMIENTOS DE CAJA").FontSize(12).Bold().FontColor("#2E4F3E");
                        if (datos.Movimientos.Count > 0)
                        {
                            col.Item().PaddingVertical(5).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background("#2E4F3E").Padding(5).Text("Responsable").FontColor(Colors.White).Bold();
                                    header.Cell().Background("#2E4F3E").Padding(5).Text("Fecha Apertura").FontColor(Colors.White).Bold();
                                    header.Cell().Background("#2E4F3E").Padding(5).Text("Monto Ini").FontColor(Colors.White).Bold();
                                    header.Cell().Background("#2E4F3E").Padding(5).Text("Fecha Cierre").FontColor(Colors.White).Bold();
                                    header.Cell().Background("#2E4F3E").Padding(5).Text("Monto Fin").FontColor(Colors.White).Bold();
                                });

                                foreach (var mov in datos.Movimientos)
                                {
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(mov.IdUsuarioNavigation?.NomUsuario ?? "N/D");
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(mov.FechaHoraApertura.ToString("dd/MM/yyyy HH:mm"));
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"$ {mov.MontoInicial.ToString("N0", cultura)}");
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(mov.EstadoDia ? "Abierta" : mov.FechaHoraCierre.ToString("dd/MM/yyyy HH:mm"));
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(mov.EstadoDia ? "--" : $"$ {mov.MontoFinal.ToString("N0", cultura)}");
                                }
                            });
                        }
                        else
                        {
                            col.Item().PaddingVertical(5).Text("No hay movimientos de caja registrados.").Italic().FontColor(Colors.Grey.Medium);
                        }

                        col.Item().PaddingTop(20).Text("FACTURAS PENDIENTES DE PAGO").FontSize(12).Bold().FontColor("#856404");
                        if (datos.FacturasPendientes.Count > 0)
                        {
                            col.Item().PaddingVertical(5).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background("#FFEeba").Padding(5).Text("ID").Bold();
                                    header.Cell().Background("#FFEeba").Padding(5).Text("Tipo").Bold();
                                    header.Cell().Background("#FFEeba").Padding(5).Text("Proveedor / Servicio").Bold();
                                    header.Cell().Background("#FFEeba").Padding(5).Text("Vencimiento").Bold();
                                    header.Cell().Background("#FFEeba").Padding(5).Text("Valor").Bold();
                                });

                                foreach (var f in datos.FacturasPendientes)
                                {
                                    bool esProveedor = f.TipFactura || (f.IdProveedor.HasValue && f.IdProveedor.Value > 0) || !string.IsNullOrWhiteSpace(f.NomProveedor);
                                    string tipo = esProveedor ? "Proveedor" : "Servicio";
                                    string nombre = esProveedor
                                        ? (!string.IsNullOrWhiteSpace(f.NomProveedor) ? f.NomProveedor! : "Proveedor")
                                        : (!string.IsNullOrWhiteSpace(f.NomServicio) ? f.NomServicio! : "Servicio");

                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(f.IdFactura.ToString());
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(tipo);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(nombre);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(f.FechaVencimiento.ToString("dd/MM/yyyy"));
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"$ {f.ValorFactura.ToString("N0", cultura)}");
                                }
                            });
                            col.Item().PaddingTop(5).AlignRight().Text($"TOTAL PENDIENTE: $ {datos.TotalFacturasPendientes.ToString("N0", cultura)}").Bold().FontColor("#856404");
                        }

                        col.Item().PaddingTop(20).Text("FACTURAS PAGADAS / CANCELADAS").FontSize(12).Bold().FontColor("#198754");
                        if (datos.FacturasPagadas.Count > 0)
                        {
                            col.Item().PaddingVertical(5).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background("#D1E7DD").Padding(5).Text("ID").Bold();
                                    header.Cell().Background("#D1E7DD").Padding(5).Text("Tipo").Bold();
                                    header.Cell().Background("#D1E7DD").Padding(5).Text("Proveedor / Servicio").Bold();
                                    header.Cell().Background("#D1E7DD").Padding(5).Text("Vencimiento").Bold();
                                    header.Cell().Background("#D1E7DD").Padding(5).Text("Valor").Bold();
                                });

                                foreach (var f in datos.FacturasPagadas)
                                {
                                    bool esProveedor = f.TipFactura || (f.IdProveedor.HasValue && f.IdProveedor.Value > 0) || !string.IsNullOrWhiteSpace(f.NomProveedor);
                                    string tipo = esProveedor ? "Proveedor" : "Servicio";
                                    string nombre = esProveedor
                                        ? (!string.IsNullOrWhiteSpace(f.NomProveedor) ? f.NomProveedor! : "Proveedor")
                                        : (!string.IsNullOrWhiteSpace(f.NomServicio) ? f.NomServicio! : "Servicio");

                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(f.IdFactura.ToString());
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(tipo);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(nombre);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(f.FechaVencimiento.ToString("dd/MM/yyyy"));
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"$ {f.ValorFactura.ToString("N0", cultura)}");
                                }
                            });
                            col.Item().PaddingTop(5).AlignRight().Text($"TOTAL PAGADO: $ {datos.TotalFacturasPagadas.ToString("N0", cultura)}").Bold().FontColor("#198754");
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Reporte generado el ").FontSize(9).FontColor(Colors.Grey.Medium);
                        x.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(9).FontColor(Colors.Grey.Medium);
                    });
                });
            }).GeneratePdf();
        }
    }
}
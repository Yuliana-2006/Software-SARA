using Microsoft.AspNetCore.Mvc;
using Software_SARA.Dtos;
using Software_SARA.Repositories.Models;
using Software_SARA.Services;
using Software_SARA.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
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

        public InformeController(SarabdContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Generar(DateTime fechaSeleccionada)
        {
            var datos = ObtenerDatosInforme(fechaSeleccionada);

            if (datos == null || (datos.Movimientos.Count == 0 && datos.FacturasPendientes.Count == 0 && datos.FacturasPagadas.Count == 0))
            {
                TempData["MensajeError"] = "No se encontraron registros para la fecha seleccionada.";
                return RedirectToAction("Index");
            }

            return View("Preview", datos);
        }

        [HttpGet]
        public IActionResult Preview(DateTime fechaSeleccionada)
        {
            var datos = ObtenerDatosInforme(fechaSeleccionada);
            if (datos == null) return RedirectToAction("Index");
            return View(datos);
        }

        [HttpGet]
        public IActionResult DescargarPDF(DateTime fechaSeleccionada)
        {
            var datos = ObtenerDatosInforme(fechaSeleccionada);
            if (datos == null) return NotFound();

            var pdfBytes = GenerarPdf(datos);
            return File(pdfBytes, "application/pdf", $"Reporte_{fechaSeleccionada:yyyyMMdd}.pdf");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnviarCorreo(DateTime fechaSeleccionada, string emailDestino)
        {
            if (string.IsNullOrEmpty(emailDestino))
            {
                TempData["MensajeError"] = "Por favor, seleccione un destinatario de la lista.";
                return RedirectToAction("Preview", new { fechaSeleccionada = fechaSeleccionada.ToString("yyyy-MM-dd") });
            }

            try
            {
                var datos = ObtenerDatosInforme(fechaSeleccionada);
                var pdfBytes = GenerarPdf(datos);

                var usuarioDestino = _context.Usuarios.FirstOrDefault(u => u.Correo == emailDestino);
                string nombreDestino = usuarioDestino != null ? $"{usuarioDestino.NomUsuario} {usuarioDestino.ApelliUsuario}" : "Usuario SARA";

                await _emailService.EnviarInformeCajaAsync(emailDestino, nombreDestino, datos.NombreSede, datos.Fecha, pdfBytes);

                TempData["MensajeExito"] = $"Reporte enviado exitosamente a {nombreDestino} ({emailDestino}).";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error al enviar el correo: " + ex.Message;
            }

            return RedirectToAction("Preview", new { fechaSeleccionada = fechaSeleccionada.ToString("yyyy-MM-dd") });
        }

        private InformeCajaDTO ObtenerDatosInforme(DateTime fecha)
        {
            var sedeIdStr = HttpContext.Session.GetString("SedeId");
            int sedeId = string.IsNullOrEmpty(sedeIdStr) ? 1 : int.Parse(sedeIdStr);

            var sede = _context.Sedes.Find(sedeId);
            var usuariosSede = _context.Usuarios.Where(u => u.IdSede == sedeId).Select(u => u.IdUsuario).ToList();

            var movimientos = _context.DiaCajas
                .Include(c => c.IdUsuarioNavigation)
                .Where(c => usuariosSede.Contains(c.IdUsuario) &&
                           (c.FechaHoraApertura.Date == fecha.Date || c.FechaHoraCierre.Date == fecha.Date))
                .ToList();

            DateOnly fechaSolo = DateOnly.FromDateTime(fecha);
            var todasFacturas = _context.Facturas
                .Where(f => usuariosSede.Contains(f.IdUsuario) &&
                           (f.FechaRegFactura == fechaSolo || f.FechaVencimiento == fechaSolo))
                .ToList();

            var proveedores = _context.Proveedors.ToList();
            var servicios = _context.Servicios.ToList();

            foreach (var f in todasFacturas)
            {
                if (f.TipFactura && f.IdProveedor.HasValue)
                {
                    var prov = proveedores.FirstOrDefault(p => p.IdProveedor == f.IdProveedor);
                    f.NomProveedor = prov != null ? prov.NomProveedor : "Sin Proveedor";
                }
                else if (!f.TipFactura && f.IdServicio.HasValue)
                {
                    var serv = servicios.FirstOrDefault(s => s.IdServicio == f.IdServicio);
                    f.NomServicio = serv != null ? serv.NomServicio : "Sin Servicio";
                }
            }

            var pendientes = todasFacturas.Where(f => !f.EstadoFactura).ToList();
            var pagadas = todasFacturas.Where(f => f.EstadoFactura).ToList();

            if (movimientos.Count == 0 && todasFacturas.Count == 0) return null;

            var usuariosCorreo = _context.Usuarios
                .Where(u => u.IdSede == sedeId &&
                           (u.IdRol == 2 || u.IdRol == 3) &&
                            u.EstadoUsuario == true)
                .ToList();

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
                                    string tipo = f.TipFactura ? "Proveedor" : "Servicio";
                                    string nombre = f.TipFactura ? (f.NomProveedor ?? "N/D") : (f.NomServicio ?? "N/D");

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
                                    string tipo = f.TipFactura ? "Proveedor" : "Servicio";
                                    string nombre = f.TipFactura ? (f.NomProveedor ?? "N/D") : (f.NomServicio ?? "N/D");

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
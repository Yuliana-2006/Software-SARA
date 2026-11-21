using Microsoft.AspNetCore.Mvc;
using Software_SARA.Dtos;
using Software_SARA.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Software_SARA.Controllers
{
    [AuthorizeRole("Tesorero")]
    public class SustentoController : Controller
    {
        private readonly IConfiguration _config;

        public SustentoController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult SeleccionarFecha()
        {
            var sedeIdStr = HttpContext.Session.GetString("SedeId");
            int sedeId = string.IsNullOrEmpty(sedeIdStr) ? 1 : int.Parse(sedeIdStr);

            var fechas = new List<DateTime>();

            try
            {
                string connectionString = _config.GetConnectionString("DefaultConnection");
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    // FILTRO CORREGIDO: estado_factura = 1 (Pendientes) y valor > 0
                    string query = @"
                        SELECT DISTINCT CAST(f.fecha_vencimiento AS DATE) AS fecha
                        FROM factura f
                        INNER JOIN usuario u ON f.id_usuario = u.id_usuario
                        WHERE f.estado_factura = 1 
                          AND f.valor_factura > 0
                          AND u.id_sede = @SedeId
                        ORDER BY fecha DESC";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@SedeId", sedeId);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (reader["fecha"] != DBNull.Value)
                                {
                                    fechas.Add(Convert.ToDateTime(reader["fecha"]));
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error al cargar las fechas: " + ex.Message;
            }

            ViewBag.FechasDisponibles = fechas;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Calcular(DateTime fechaSeleccionada)
        {
            var sedeIdStr = HttpContext.Session.GetString("SedeId");
            int sedeId = string.IsNullOrEmpty(sedeIdStr) ? 1 : int.Parse(sedeIdStr);

            try
            {
                var listaDto = new List<SustentoItemDTO>();
                decimal totalSustento = 0;

                string connectionString = _config.GetConnectionString("DefaultConnection");

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    // FILTRO CORREGIDO: estado_factura = 1 (Pendientes)
                    string query = @"
                        SELECT 
                            f.id_factura, 
                            f.tip_factura, 
                            f.id_proveedor,
                            f.id_servicio,
                            f.fecha_vencimiento, 
                            f.valor_factura,
                            p.nom_proveedor,
                            s.nom_servicio
                        FROM factura f
                        LEFT JOIN proveedor p ON f.id_proveedor = p.id_proveedor
                        LEFT JOIN servicio s ON f.id_servicio = s.id_servicio
                        INNER JOIN usuario u ON f.id_usuario = u.id_usuario
                        WHERE f.estado_factura = 1 
                          AND f.valor_factura > 0
                          AND u.id_sede = @SedeId
                          AND CAST(f.fecha_vencimiento AS DATE) = CAST(@FechaSeleccionada AS DATE)";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@SedeId", sedeId);
                        cmd.Parameters.AddWithValue("@FechaSeleccionada", fechaSeleccionada);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string? nomProveedor = reader["nom_proveedor"] != DBNull.Value
                                    ? reader["nom_proveedor"].ToString()?.Trim()
                                    : null;

                                string? nomServicio = reader["nom_servicio"] != DBNull.Value
                                    ? reader["nom_servicio"].ToString()?.Trim()
                                    : null;

                                bool tipFactura = reader["tip_factura"] != DBNull.Value && Convert.ToBoolean(reader["tip_factura"]);
                                bool tieneIdProveedor = reader["id_proveedor"] != DBNull.Value;
                                bool tieneIdServicio = reader["id_servicio"] != DBNull.Value;

                                bool esProveedor = !string.IsNullOrWhiteSpace(nomProveedor) || (tieneIdProveedor && !tieneIdServicio) || tipFactura;

                                string tipoStr;
                                string nombreTercero;

                                if (esProveedor && !string.IsNullOrWhiteSpace(nomProveedor))
                                {
                                    tipoStr = "Proveedor";
                                    nombreTercero = nomProveedor;
                                }
                                else if (!esProveedor && !string.IsNullOrWhiteSpace(nomServicio))
                                {
                                    tipoStr = "Servicio";
                                    nombreTercero = nomServicio;
                                }
                                else if (!string.IsNullOrWhiteSpace(nomProveedor))
                                {
                                    tipoStr = "Proveedor";
                                    nombreTercero = nomProveedor;
                                }
                                else if (!string.IsNullOrWhiteSpace(nomServicio))
                                {
                                    tipoStr = "Servicio";
                                    nombreTercero = nomServicio;
                                }
                                else
                                {
                                    tipoStr = tipFactura ? "Proveedor" : "Servicio";
                                    nombreTercero = tipFactura ? "Proveedor sin nombre" : "Servicio sin nombre";
                                }

                                var item = new SustentoItemDTO
                                {
                                    IdFactura = Convert.ToInt32(reader["id_factura"]),
                                    Tipo = tipoStr,
                                    NombreTercero = nombreTercero,
                                    FechaVencimiento = Convert.ToDateTime(reader["fecha_vencimiento"]),
                                    Valor = Convert.ToDecimal(reader["valor_factura"])
                                };

                                listaDto.Add(item);
                                totalSustento += item.Valor;
                            }
                        }
                    }
                }

                var dto = new SustentoDiarioDTO
                {
                    Facturas = listaDto,
                    TotalSustento = totalSustento,
                    FechaCalculo = fechaSeleccionada
                };

                return View(dto);
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Error real (SQL): " + ex.Message;
                return RedirectToAction("Index");
            }
        }
    }
}
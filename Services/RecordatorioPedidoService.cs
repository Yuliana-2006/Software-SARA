using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Software_SARA.Dtos;
using Software_SARA.Repositories;

namespace Software_SARA.Services
{
    public class RecordatorioPedidoService
    {
        private readonly RecordatorioPedidoRepository _repository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RecordatorioPedidoService(RecordatorioPedidoRepository repository, IHttpContextAccessor httpContextAccessor)
        {
            _repository = repository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<RecordatorioPedidoDto>> ObtenerTodosLosRecordatorios()
        {
            var resultado = await _repository.ObtenerTodosLosRecordatorios();
            return resultado ?? new List<RecordatorioPedidoDto>();
        }

        public async Task<string> ObtenerSiguienteId()
        {
            var lista = await _repository.ObtenerTodosLosRecordatorios();
            int siguiente = lista.Count + 1;
            return siguiente.ToString("D3");
        }

        public List<SelectListItem> ObtenerListaProveedores()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "Colombina" },
                new SelectListItem { Value = "2", Text = "Coca Cola" },
                new SelectListItem { Value = "3", Text = "Postobón" },
                new SelectListItem { Value = "4", Text = "Nutresa" },
                new SelectListItem { Value = "5", Text = "Surtilider" },
                new SelectListItem { Value = "6", Text = "Nexpress" },
                new SelectListItem { Value = "7", Text = "Harinera del Valle" },
                new SelectListItem { Value = "8", Text = "Bimbo" },
                new SelectListItem { Value = "9", Text = "Ramo" },
                new SelectListItem { Value = "10", Text = "Sulicor" },
                new SelectListItem { Value = "11", Text = "Hielo polar" },
                new SelectListItem { Value = "12", Text = "Casa Luker" },
                new SelectListItem { Value = "13", Text = "Diana" },
                new SelectListItem { Value = "14", Text = "Alpina" },
                new SelectListItem { Value = "15", Text = "Organización Cárdenas" },
                new SelectListItem { Value = "16", Text = "Alquería" },
                new SelectListItem { Value = "17", Text = "Altipal" },
                new SelectListItem { Value = "18", Text = "Colanta" },
                new SelectListItem { Value = "19", Text = "Super Ricas" },
                new SelectListItem { Value = "20", Text = "Margarita" },
                new SelectListItem { Value = "21", Text = "Yupi" },
                new SelectListItem { Value = "22", Text = "Big Cola" },
                new SelectListItem { Value = "23", Text = "Tecnoquímica" },
                new SelectListItem { Value = "24", Text = "Corbeta" }
            };
        }

        public async Task<RecordatorioPedidoDto> CrearRecordatorio(RecordatorioPedidoDto dto)
        {
            if (dto.FechaEntrega < DateOnly.FromDateTime(DateTime.Today))
            {
                dto.Response = 0;
                dto.Message = "La fecha de entrega debe ser igual o posterior al día actual.";
                return dto;
            }

            bool existe = await _repository.ExisteIdRecordatorio(dto.IdRecordatorio);
            if (existe)
            {
                dto.Response = 0;
                dto.Message = "El ID del recordatorio ya se encuentra registrado.";
                return dto;
            }

            try
            {
                string idUsuarioStr = _httpContextAccessor.HttpContext?.Session.GetString("UsuarioId");

                int idUsuarioFinal = 1;
                if (!string.IsNullOrEmpty(idUsuarioStr) && int.TryParse(idUsuarioStr, out int parsedId))
                {
                    idUsuarioFinal = parsedId;
                }
                else
                {
                    dto.Response = 0;
                    dto.Message = "Error: No se encontró una sesión activa. Inicie sesión nuevamente.";
                    return dto;
                }

                // Asignación automática del estado a '0' (Pendiente) al crear
                dto.Estado = "0";

                bool creado = await _repository.CrearRecordatorio(dto, idUsuarioFinal);
                if (creado)
                {
                    dto.Response = 1;
                    dto.Message = "Recordatorio de pedido agregado exitosamente.";
                }
                else
                {
                    dto.Response = 0;
                    dto.Message = "No fue posible agregar el recordatorio. Intente nuevamente.";
                }
            }
            catch (Exception ex)
            {
                dto.Response = 0;
                dto.Message = "Error técnico: " + ex.Message;
            }

            return dto;
        }

        public async Task<RecordatorioPedidoDto> ObtenerRecordatorioPorId(int id)
        {
            return await _repository.ObtenerPorId(id);
        }

        public async Task<RecordatorioPedidoDto> ActualizarRecordatorio(RecordatorioPedidoDto dto)
        {
            if (dto.FechaEntrega < DateOnly.FromDateTime(DateTime.Today))
            {
                dto.Response = 0;
                dto.Message = "La fecha de entrega debe ser igual o posterior al día actual.";
                return dto;
            }

            try
            {
                bool actualizado = await _repository.ActualizarRecordatorio(dto);
                if (actualizado)
                {
                    dto.Response = 1;
                    dto.Message = "Recordatorio de pedido actualizado exitosamente.";
                }
                else
                {
                    dto.Response = 0;
                    dto.Message = "No fue posible actualizar el recordatorio. Intente nuevamente.";
                }
            }
            catch (Exception ex)
            {
                dto.Response = 0;
                dto.Message = "Error técnico: " + ex.Message;
            }

            return dto;
        }

        public async Task<bool> ConfirmarEntrega(int idRecordatorio)
        {
            return await _repository.ActualizarEstadoRecordatorio(idRecordatorio, 1);
        }

        // ==========================================
        // LÓGICA DE NOTIFICACIONES (10, 5, 3 días y el día de entrega)
        // ==========================================
        public async Task<List<RecordatorioPedidoDto>> ObtenerEntregasProximasNotificaciones()
        {
            var todos = await _repository.ObtenerTodosLosRecordatorios();
            var hoy = DateOnly.FromDateTime(DateTime.Today);
            var listaAlertas = new List<RecordatorioPedidoDto>();

            foreach (var rec in todos)
            {
                if (rec.Estado.Equals("Pendiente", StringComparison.OrdinalIgnoreCase))
                {
                    int diasRestantes = rec.FechaEntrega.DayNumber - hoy.DayNumber;

                    // Hitos exactos solicitados: 10, 5, 3 días o el mismo día / vencido (<= 0)
                    if (diasRestantes == 10 || diasRestantes == 5 || diasRestantes == 3 || diasRestantes <= 0)
                    {
                        rec.Estado = diasRestantes == 0 ? "¡Vence hoy!" : (diasRestantes < 0 ? $"Vencido hace {Math.Abs(diasRestantes)} días" : $"Faltan {diasRestantes} días");
                        listaAlertas.Add(rec);
                    }
                }
            }

            return listaAlertas;
        }
    }
}
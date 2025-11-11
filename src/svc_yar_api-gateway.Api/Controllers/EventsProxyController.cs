using System.Net.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace svc_yar_api_gateway.Api.Controllers
{
    /// <summary>
    /// Controlador proxy que reenvía requests al servicio de eventos
    /// </summary>
    [ApiController]
    [Route("api/eventos")]
    public class EventsProxyController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<EventsProxyController> _logger;

        public EventsProxyController(
            IHttpClientFactory httpClientFactory,
            ILogger<EventsProxyController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene eventos publicados (endpoint público, no requiere autenticación)
        /// Si el servicio backend no está disponible, retorna datos mock
        /// </summary>
        /// <returns>Lista de eventos publicados del servicio backend o datos mock</returns>
        [HttpGet("publicados")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicEvents()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("eventos");

                // Forward the incoming query string
                var path = "/api/eventos/publicados" + (Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty);

                var requestMessage = new HttpRequestMessage(HttpMethod.Get, path);

                // Forward correlation id if present
                if (Request.Headers.TryGetValue("X-Correlation-ID", out var correlation))
                {
                    requestMessage.Headers.Add("X-Correlation-ID", correlation.ToString());
                }

                _logger.LogInformation("Proxying request to eventos service: {Path}", path);

                // Configurar timeout más corto para detectar servicios no disponibles rápidamente
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var response = await client.SendAsync(requestMessage, cts.Token);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Eventos service returned error status {StatusCode} for path {Path}",
                        response.StatusCode,
                        path);
                }

                return new ContentResult
                {
                    Content = content,
                    StatusCode = (int)response.StatusCode,
                    ContentType = "application/json"
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Eventos service not available, returning mock data");
                return GetMockPublicEvents();
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Eventos service timeout, returning mock data");
                return GetMockPublicEvents();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error accessing eventos service, returning mock data");
                return GetMockPublicEvents();
            }
        }

        /// <summary>
        /// Retorna datos mock cuando el servicio real no está disponible
        /// </summary>
        private IActionResult GetMockPublicEvents()
        {
            var categoria = Request.Query["categoria"].ToString();
            var page = int.TryParse(Request.Query["page"], out var p) ? p : 1;
            var pageSize = int.TryParse(Request.Query["pageSize"], out var ps) ? ps : 20;

            var mockEventos = new List<object>
            {
                new
                {
                    id = "1",
                    nombre = "Concierto de Rock",
                    descripcion = "Un concierto épico de rock con las mejores bandas",
                    categoria = "musica",
                    fecha = DateTime.UtcNow.AddDays(30).ToString("O"),
                    venue = "Estadio Nacional",
                    precio = 50.00,
                    aforo = 5000,
                    aforoDisponible = 3500,
                    estado = "publicado",
                    imagen = "https://example.com/rock-concert.jpg",
                    organizadorId = "org-123"
                },
                new
                {
                    id = "2",
                    nombre = "Festival de Jazz",
                    descripcion = "Disfruta de los mejores músicos de jazz",
                    categoria = "musica",
                    fecha = DateTime.UtcNow.AddDays(45).ToString("O"),
                    venue = "Teatro Municipal",
                    precio = 35.00,
                    aforo = 1000,
                    aforoDisponible = 750,
                    estado = "publicado",
                    imagen = "https://example.com/jazz-festival.jpg",
                    organizadorId = "org-456"
                },
                new
                {
                    id = "3",
                    nombre = "Conferencia de Tecnología",
                    descripcion = "Las últimas tendencias en tecnología",
                    categoria = "tecnologia",
                    fecha = DateTime.UtcNow.AddDays(20).ToString("O"),
                    venue = "Centro de Convenciones",
                    precio = 100.00,
                    aforo = 2000,
                    aforoDisponible = 1500,
                    estado = "publicado",
                    imagen = "https://example.com/tech-conference.jpg",
                    organizadorId = "org-789"
                },
                new
                {
                    id = "4",
                    nombre = "Maratón de la Ciudad",
                    descripcion = "Carrera de 42km por la ciudad",
                    categoria = "deportes",
                    fecha = DateTime.UtcNow.AddDays(60).ToString("O"),
                    venue = "Parque Central",
                    precio = 25.00,
                    aforo = 10000,
                    aforoDisponible = 8000,
                    estado = "publicado",
                    imagen = "https://example.com/marathon.jpg",
                    organizadorId = "org-321"
                },
                new
                {
                    id = "5",
                    nombre = "Obra de Teatro: Hamlet",
                    descripcion = "Clásica obra de Shakespeare",
                    categoria = "teatro",
                    fecha = DateTime.UtcNow.AddDays(15).ToString("O"),
                    venue = "Teatro Principal",
                    precio = 40.00,
                    aforo = 500,
                    aforoDisponible = 300,
                    estado = "publicado",
                    imagen = "https://example.com/hamlet.jpg",
                    organizadorId = "org-654"
                }
            };

            // Filtrar por categoría si se proporciona
            if (!string.IsNullOrEmpty(categoria))
            {
                mockEventos = mockEventos
                    .Where(e => e.GetType().GetProperty("categoria")?.GetValue(e)?.ToString()?.ToLower() == categoria.ToLower())
                    .ToList();
            }

            // Paginación simple
            var total = mockEventos.Count;
            var skip = (page - 1) * pageSize;
            var paginatedEventos = mockEventos.Skip(skip).Take(pageSize).ToList();

            return Ok(new
            {
                data = paginatedEventos,
                page = page,
                pageSize = pageSize,
                total = total,
                totalPages = (int)Math.Ceiling(total / (double)pageSize),
                _mock = true // Indicador de que son datos mock
            });
        }

        /// <summary>
        /// Obtiene un evento por ID (requiere autenticación)
        /// </summary>
        /// <param name="id">ID del evento</param>
        /// <returns>Detalles del evento</returns>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetEventById(string id)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("eventos");
                var path = $"/api/eventos/{id}" + (Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty);

                var requestMessage = new HttpRequestMessage(HttpMethod.Get, path);

                // Forward correlation id
                if (Request.Headers.TryGetValue("X-Correlation-ID", out var correlation))
                {
                    requestMessage.Headers.Add("X-Correlation-ID", correlation.ToString());
                }

                // Forward Authorization header to backend service
                if (Request.Headers.TryGetValue("Authorization", out var authHeader))
                {
                    requestMessage.Headers.Add("Authorization", authHeader.ToString());
                }

                _logger.LogInformation("Proxying authenticated request to eventos service: {Path}", path);

                var response = await client.SendAsync(requestMessage);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Eventos service returned error status {StatusCode} for path {Path}",
                        response.StatusCode,
                        path);
                }

                return new ContentResult
                {
                    Content = content,
                    StatusCode = (int)response.StatusCode,
                    ContentType = "application/json"
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error communicating with eventos service");
                return StatusCode(502, new { error = "Error al comunicarse con el servicio de eventos", message = ex.Message });
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Timeout communicating with eventos service");
                return StatusCode(504, new { error = "Timeout al comunicarse con el servicio de eventos" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetEventById");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }
    }
}

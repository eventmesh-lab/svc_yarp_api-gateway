using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace svc_yar_api_gateway.Api.Controllers
{
    /// <summary>
    /// Controlador mock para simular el servicio de eventos cuando no está disponible.
    /// Este controlador solo se usa en modo desarrollo cuando USE_MOCK_SERVICE=true
    /// </summary>
    [ApiController]
    [Route("mock/api/eventos")]
    public class MockEventosController : ControllerBase
    {
        private readonly ILogger<MockEventosController> _logger;

        public MockEventosController(ILogger<MockEventosController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Mock del endpoint de eventos publicados
        /// </summary>
        [HttpGet("publicados")]
        [AllowAnonymous]
        public IActionResult GetPublicEvents([FromQuery] string? categoria = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            _logger.LogInformation("Mock service: Returning mock eventos publicados");

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
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
            });
        }

        /// <summary>
        /// Mock del endpoint para obtener un evento por ID
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public IActionResult GetEventById(string id)
        {
            _logger.LogInformation("Mock service: Returning mock evento with id {Id}", id);

            var mockEvento = new
            {
                id = id,
                nombre = $"Evento {id}",
                descripcion = $"Descripción detallada del evento {id}",
                categoria = "musica",
                fecha = DateTime.UtcNow.AddDays(30).ToString("O"),
                venue = "Venue Principal",
                precio = 50.00,
                aforo = 5000,
                aforoDisponible = 3500,
                estado = "publicado",
                imagen = $"https://example.com/event-{id}.jpg",
                organizadorId = "org-123",
                detalles = new
                {
                    duracion = "3 horas",
                    restricciones = "Mayores de 18 años",
                    incluye = new[] { "Entrada", "Estacionamiento" }
                }
            };

            return Ok(mockEvento);
        }
    }
}


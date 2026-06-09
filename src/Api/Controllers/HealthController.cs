// Api/Controllers/HealthController.cs
// Endpoint de health check simple para monitoreo.
// Usado por orquestadores (Docker, Kubernetes) y balanceadores de carga
// para verificar que la aplicación está viva y respondiendo.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UsersApi.Api.Controllers;

[ApiController]
[Route("[controller]")]
// AllowAnonymous: el health check debe ser público, sin token JWT.
[AllowAnonymous]
public class HealthController : ControllerBase
{
    // GET /health
    // Retorna 200 OK con información básica de estado.
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "Healthy",
            timestamp = DateTime.UtcNow
        });
    }
}

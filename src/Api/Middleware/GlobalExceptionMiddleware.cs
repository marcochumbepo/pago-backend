// Api/Middleware/GlobalExceptionMiddleware.cs
// Middleware único para el manejo global de errores.
// Centraliza toda la lógica de captura de excepciones en un solo punto,
// garantizando respuestas de error consistentes en toda la API.
// 
// Beneficios:
// - Evita try/catch repetitivos en cada controller/service.
// - Oculta stack traces en producción (seguridad).
// - Formato de respuesta estandarizado para los clientes.
// - Logging centralizado de errores no controlados.
using System.Net;
using System.Text.Json;
using UsersApi.Application.Exceptions;

namespace UsersApi.Api.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Continúa con el siguiente middleware/controller en el pipeline.
            await _next(context);
        }
        catch (ValidationException ex)
        {
            // Error 400: datos de entrada inválidos.
            // Se retornan los errores de validación campo por campo para que el cliente
            // pueda mostrar mensajes específicos en el formulario.
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";

            var response = new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                title = "Error de validación",
                status = (int)HttpStatusCode.BadRequest,
                errors = ex.Errors
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
        catch (NotFoundException ex)
        {
            // Error 404: recurso no encontrado.
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            context.Response.ContentType = "application/json";

            var response = new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                title = "Recurso no encontrado",
                status = (int)HttpStatusCode.NotFound,
                detail = ex.Message
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
        catch (UnauthorizedAccessException ex)
        {
            // Error 401: credenciales inválidas en el login.
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Response.ContentType = "application/json";

            var response = new
            {
                type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                title = "No autorizado",
                status = (int)HttpStatusCode.Unauthorized,
                detail = ex.Message
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            // Error 500: cualquier excepción no controlada.
            // Se registra el stack trace completo internamente, pero solo se retorna
            // un mensaje genérico al cliente por seguridad (no exponer detalles internos).
            _logger.LogError(ex, "Error no controlado: {Message}", ex.Message);

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var response = new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                title = "Error interno del servidor",
                status = (int)HttpStatusCode.InternalServerError,
                detail = "Ha ocurrido un error inesperado. Contacte al administrador."
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}

// Método de extensión para registrar el middleware de forma limpia en Program.cs.
public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionMiddleware>();
    }
}

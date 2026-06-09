// Application/Exceptions/ValidationException.cs
// Excepción de dominio para errores de validación.
// El GlobalExceptionMiddleware la captura y retorna HTTP 400 con los detalles de validación.
namespace UsersApi.Application.Exceptions;

public class ValidationException : Exception
{
    // Almacena los errores de validación como diccionario para que el middleware
    // pueda serializarlos en un formato amigable para el cliente (campo -> mensajes).
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(string message, IDictionary<string, string[]> errors)
        : base(message)
    {
        Errors = errors;
    }
}

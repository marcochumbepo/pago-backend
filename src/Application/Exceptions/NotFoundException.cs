// Application/Exceptions/NotFoundException.cs
// Excepción de dominio para cuando un recurso solicitado no existe.
// El GlobalExceptionMiddleware la captura y retorna HTTP 404 de forma consistente.
namespace UsersApi.Application.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

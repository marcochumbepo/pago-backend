// Application/Validators/CreateUserRequestValidator.cs
// Implementa las reglas de validación usando FluentValidation.
// Mantiene las validaciones separadas del controller (Single Responsibility),
// facilitando el testing unitario de las reglas de negocio sin dependencias HTTP.
using FluentValidation;
using UsersApi.Application.DTOs;

namespace UsersApi.Application.Validators;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        // FirstName: obligatorio para garantizar integridad de datos.
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("El nombre es obligatorio.");

        // LastName: obligatorio para garantizar integridad de datos.
        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("El apellido es obligatorio.");

        // Email: obligatorio + formato válido según RFC.
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio.")
            .EmailAddress().WithMessage("El formato del email no es válido.");
    }
}

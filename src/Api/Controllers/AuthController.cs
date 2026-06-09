// Api/Controllers/AuthController.cs
// Controlador de autenticación: endpoint público para obtener token JWT.
// 
// Flujo:
// 1. Cliente envía POST /api/auth/login con username y password.
// 2. AuthService valida credenciales y genera JWT.
// 3. Cliente usa el token en el header Authorization: Bearer <token> para
//    acceder a los endpoints protegidos de UsersController.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UsersApi.Application.DTOs;
using UsersApi.Application.Services;

namespace UsersApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
// AllowAnonymous a nivel de controlador porque el login debe ser público.
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    // POST /api/auth/login
    // Retorna 200 con el token JWT y su fecha de expiración.
    // Retorna 401 si las credenciales son inválidas.
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
    {
        var response = _authService.Execute(request);
        return Ok(response);
    }
}

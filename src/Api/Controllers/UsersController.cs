// Api/Controllers/UsersController.cs
// Controller para el recurso Users. Sigue el patrón API Controller de ASP.NET Core.
// Responsabilidades limitadas a:
// 1. Recibir y validar el request HTTP.
// 2. Delegar la lógica de negocio a los servicios de Application.
// 3. Devolver la respuesta HTTP adecuada.
// No contiene lógica de negocio (Single Responsibility).
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UsersApi.Application.DTOs;
using UsersApi.Application.Services;

namespace UsersApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize] protege todos los endpoints de este controlador.
// Solo requests con token JWT válido (Authorization: Bearer <token>) pueden acceder.
[Authorize]
public class UsersController : ControllerBase
{
    private readonly CreateUserService _createUserService;
    private readonly GetUsersService _getUsersService;
    private readonly GetUserByIdService _getUserByIdService;
    private readonly DeleteUserService _deleteUserService;

    public UsersController(
        CreateUserService createUserService,
        GetUsersService getUsersService,
        GetUserByIdService getUserByIdService,
        DeleteUserService deleteUserService)
    {
        _createUserService = createUserService;
        _getUsersService = getUsersService;
        _getUserByIdService = getUserByIdService;
        _deleteUserService = deleteUserService;
    }

    // POST /api/users
    // Crea un nuevo usuario. Retorna 201 Created con la ubicación del recurso. 
    [HttpPost]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserResponse>> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _createUserService.ExecuteAsync(request, cancellationToken);

        // Retorna 201 + Location header (RESTful) + el recurso creado en el body. 
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    // GET /api/users
    // Obtiene todos los usuarios. Retorna 200 OK con la lista.
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UserResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<UserResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var users = await _getUsersService.ExecuteAsync(cancellationToken);
        return Ok(users);
    }

    // GET /api/users/{id}
    // Obtiene un usuario por ID. Retorna 200 OK o 404 si no existe.
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var user = await _getUserByIdService.ExecuteAsync(id, cancellationToken);
        return Ok(user);
    }

    // DELETE /api/users/{id}
    // Elimina un usuario por ID. Retorna 204 No Content o 404 si no existe.
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _deleteUserService.ExecuteAsync(id, cancellationToken);
        return NoContent();
    }
}

using Clinic.Application.Features.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IMediator mediator) : ControllerBase
{
    /// <summary>Staff login (username + password) → JWT.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResult>> Login([FromBody] LoginCommand command, CancellationToken ct)
        => Ok(await mediator.Send(command, ct));

    /// <summary>Returns the current authenticated user (used on app load / refresh).</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(AuthResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResult>> Me(CancellationToken ct)
        => Ok(await mediator.Send(new GetCurrentUserQuery(), ct));
}

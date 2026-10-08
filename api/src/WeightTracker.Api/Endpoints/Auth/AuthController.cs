using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeightTracker.Api.Common;
using WeightTracker.Api.Endpoints.Auth.Dtos;
using WeightTracker.Application.UseCases.Auth;

namespace WeightTracker.Api.Endpoints.Auth;

[ApiController]
[Route("auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly LoginUseCase _login;
    private readonly RefreshTokenUseCase _refresh;
    private readonly ExceptionMapper _exceptionMapper;

    public AuthController(
        LoginUseCase login,
        RefreshTokenUseCase refresh,
        ExceptionMapper exceptionMapper)
    {
        _login = login;
        _refresh = refresh;
        _exceptionMapper = exceptionMapper;
    }

    /// <summary>
    /// Exchanges credentials for an access token and a refresh token.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _login.Execute(request.Email, request.Password, cancellationToken);

            return Ok(AuthResponse.From(result));
        }
        catch (Exception exception)
        {
            return _exceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Redeems a refresh token for a fresh credential pair. The presented token is revoked.
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _refresh.Execute(request.RefreshToken, cancellationToken);

            return Ok(AuthResponse.From(result));
        }
        catch (Exception exception)
        {
            return _exceptionMapper.Map(exception);
        }
    }
}

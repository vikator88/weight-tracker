using Microsoft.AspNetCore.Mvc;
using WeightTracker.Api.Common;
using WeightTracker.Api.Endpoints.Users.Dtos;
using WeightTracker.Application.UseCases.Auth;
using WeightTracker.Application.UseCases.Users;

namespace WeightTracker.Api.Endpoints.Users;

/// <summary>
/// The caller's own profile. Both routes act on the user the token belongs to; no route takes
/// a user id.
/// </summary>
[ApiController]
[Route("users")]
public class UsersController : AuthorizedController
{
    private readonly UpdateMyProfileUseCase _updateMyProfile;

    public UsersController(
        UpdateMyProfileUseCase updateMyProfile,
        GetAuthenticatedUserUseCase getAuthenticatedUser,
        ExceptionMapper exceptionMapper) : base(getAuthenticatedUser, exceptionMapper)
    {
        _updateMyProfile = updateMyProfile;
    }

    /// <summary>
    /// Reads the caller's stored profile. It is the user already resolved for this request.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetMe()
    {
        try
        {
            return Ok(UserProfileResponse.From(CurrentUser!));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Replaces the caller's name and surname. Email and role cannot be changed here.
    /// </summary>
    [HttpPut("me")]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateMe(
        [FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _updateMyProfile.Execute(CurrentUser!.Id, request.ToDto(), cancellationToken);

            return Ok(UserProfileResponse.From(user));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }
}

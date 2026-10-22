using Clinic.Application.Abstractions;
using Clinic.Application.Common.Interfaces;
using FluentValidation;
using MediatR;

namespace Clinic.Application.Features.Auth;

public record GetCurrentUserQuery : IRequest<AuthResult>;

public class GetCurrentUserQueryHandler(
    ICurrentUserService current,
    IIdentityService identity,
    IJwtTokenService tokens) : IRequestHandler<GetCurrentUserQuery, AuthResult>
{
    public async Task<AuthResult> Handle(GetCurrentUserQuery request, CancellationToken ct)
    {
        if (!current.IsAuthenticated || current.UserId is null)
            throw new Common.Exceptions.ForbiddenException("Not authenticated.");

        var user = await identity.FindByNameAsync(current.UserName!);
        if (user is null || !user.IsActive)
            throw new Common.Exceptions.NotFoundException("User", current.UserId);

        var roles = await identity.GetRolesAsync(user);
        var token = await tokens.IssueTokenAsync(user, ct);
        return new AuthResult(token, new UserBrief(user.ExternalId, user.UserName!, user.FullName, user.Email!, roles));
    }
}

using Clinic.Application.Abstractions;
using Clinic.Application.Common.Interfaces;
using Clinic.Domain.Entities;
using FluentValidation;
using MediatR;

namespace Clinic.Application.Features.Auth;

public record LoginCommand(string Username, string Password) : IRequest<AuthResult>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
    }
}

public class LoginCommandHandler(
    IIdentityService identity,
    IJwtTokenService tokens) : IRequestHandler<LoginCommand, AuthResult>
{
    public async Task<AuthResult> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await identity.FindByNameAsync(request.Username);
        if (user is null || !user.IsActive)
            throw new Common.Exceptions.AppException("Invalid username or password.");

        var ok = await identity.CheckPasswordSignInAsync(user, request.Password);
        if (!ok)
            throw new Common.Exceptions.AppException("Invalid username or password.");

        var roles = await identity.GetRolesAsync(user);
        var token = await tokens.IssueTokenAsync(user, ct);

        return new AuthResult(
            token,
            new UserBrief(user.ExternalId, user.UserName!, user.FullName, user.Email!, roles));
    }
}

public record AuthResult(string Token, UserBrief User);

public record UserBrief(Guid Id, string Username, string FullName, string Email, IList<string> Roles);

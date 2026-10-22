namespace Clinic.Application.Common.Exceptions;

public class AppException : Exception
{
    public AppException(string message) : base(message) { }
    public AppException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>Resource was not found by ExternalId or Id.</summary>
public class NotFoundException : AppException
{
    public NotFoundException(string entity, object key)
        : base($"'{entity}' with key '{key}' was not found.") { }
}

/// <summary>Authorization failed for the current user.</summary>
public class ForbiddenException : AppException
{
    public ForbiddenException(string message = "You are not authorized to perform this action.")
        : base(message) { }
}

/// <summary>One or more validation failures produced by FluentValidation.</summary>
public class ValidationException : AppException
{
    public IDictionary<string, string[]> Errors { get; } = new Dictionary<string, string[]>();

    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation failures have occurred.") => Errors = errors;
}

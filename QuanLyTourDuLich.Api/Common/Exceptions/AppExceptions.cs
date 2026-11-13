namespace QuanLyTourDuLich.Api.Common.Exceptions;

public abstract class AppException(int statusCode, string errorCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string ErrorCode { get; } = errorCode;
}

public sealed class BadRequestException(string message, string errorCode = "bad_request")
    : AppException(StatusCodes.Status400BadRequest, errorCode, message);

public sealed class UnauthorizedException(string message, string errorCode = "unauthorized")
    : AppException(StatusCodes.Status401Unauthorized, errorCode, message);

public sealed class ForbiddenException(string message, string errorCode = "forbidden")
    : AppException(StatusCodes.Status403Forbidden, errorCode, message);

public sealed class NotFoundException(string message, string errorCode = "not_found")
    : AppException(StatusCodes.Status404NotFound, errorCode, message);

public sealed class ConflictException(string message, string errorCode = "conflict")
    : AppException(StatusCodes.Status409Conflict, errorCode, message);

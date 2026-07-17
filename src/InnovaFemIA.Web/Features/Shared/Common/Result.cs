namespace InnovaFemIA.Web.Features.Shared.Common;

/// <summary>
/// Patrón Result para encapsular el resultado de operaciones en los handlers de MediatR.
/// Evita lanzar excepciones para errores esperados (validaciones, not found, etc.).
/// </summary>
/// <typeparam name="T">Tipo del valor retornado en caso de éxito.</typeparam>
public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Value { get; private set; }
    public string? Error { get; private set; }
    public int StatusCode { get; private set; }

    private Result() { }

    public static Result<T> Success(T value) =>
        new() { IsSuccess = true, Value = value, StatusCode = 200 };

    public static Result<T> NotFound(string message = "Recurso no encontrado") =>
        new() { IsSuccess = false, Error = message, StatusCode = 404 };

    public static Result<T> BadRequest(string message) =>
        new() { IsSuccess = false, Error = message, StatusCode = 400 };

    public static Result<T> Unauthorized(string message = "No autorizado") =>
        new() { IsSuccess = false, Error = message, StatusCode = 401 };

    public static Result<T> Failure(string message) =>
        new() { IsSuccess = false, Error = message, StatusCode = 500 };
}

/// <summary>
/// Variante no genérica del Result para comandos que no retornan valor.
/// </summary>
public class Result
{
    public bool IsSuccess { get; private set; }
    public string? Error { get; private set; }
    public int StatusCode { get; private set; }

    private Result() { }

    public static Result Success() =>
        new() { IsSuccess = true, StatusCode = 200 };

    public static Result NotFound(string message = "Recurso no encontrado") =>
        new() { IsSuccess = false, Error = message, StatusCode = 404 };

    public static Result BadRequest(string message) =>
        new() { IsSuccess = false, Error = message, StatusCode = 400 };

    public static Result Unauthorized(string message = "No autorizado") =>
        new() { IsSuccess = false, Error = message, StatusCode = 401 };

    public static Result Failure(string message) =>
        new() { IsSuccess = false, Error = message, StatusCode = 500 };
}

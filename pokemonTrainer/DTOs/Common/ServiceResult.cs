namespace pokemonTrainer.DTOs.Common;

public class ServiceResult<T>
{
    public bool Success { get; set; }

    public string? ErrorCode { get; set; }

    public string? Message { get; set; }

    public T? Data { get; set; }

    public static ServiceResult<T> Ok(T data)
    {
        return new ServiceResult<T>
        {
            Success = true,
            Data = data
        };
    }

    public static ServiceResult<T> Fail(
        string errorCode,
        string message)
    {
        return new ServiceResult<T>
        {
            Success = false,
            ErrorCode = errorCode,
            Message = message
        };
    }
}
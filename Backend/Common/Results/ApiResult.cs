using System.Collections.Generic;

namespace SmartSupermarket.Backend.Common.Results;

public class ApiResult<T>
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }

    public static ApiResult<T> Success(T data, string message = "Thành công")
    {
        return new ApiResult<T> { IsSuccess = true, Data = data, Message = message };
    }

    public static ApiResult<T> Failure(string message, List<string>? errors = null)
    {
        return new ApiResult<T> { IsSuccess = false, Message = message, Errors = errors };
    }
}

public class ApiResult : ApiResult<object>
{
    public static ApiResult Success(string message = "Thành công")
    {
        return new ApiResult { IsSuccess = true, Message = message };
    }

    public static new ApiResult Success(object? data, string message = "Thành công")
    {
        return new ApiResult { IsSuccess = true, Data = data, Message = message };
    }

    public static new ApiResult Failure(string message, List<string>? errors = null)
    {
        return new ApiResult { IsSuccess = false, Message = message, Errors = errors };
    }
}

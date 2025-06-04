namespace PlantApp.Models;

public class ApiResponse<T>
{
    public ApiResult<T>? Result { get; set; }
}

public class ApiResult<T>
{
    public string? Status { get; set; }
    public string? Message { get; set; }
    public int TotalMo { get; set; }
    public T? Data { get; set; }
}
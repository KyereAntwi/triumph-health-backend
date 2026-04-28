namespace Triumph.HealthMS.Application.Utility;

public class BaseResponse<TResponse>
{
    public string Message  { get; set; }
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; }
    public TResponse Data { get; set; }
    public IEnumerable<string> Errors { get; set; } = [];
}
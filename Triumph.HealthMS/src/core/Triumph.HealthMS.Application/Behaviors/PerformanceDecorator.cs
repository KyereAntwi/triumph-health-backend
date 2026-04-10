namespace Triumph.HealthMS.Application.Behaviors;

public class PerformanceDecorator<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IQuery<TResponse>
{
    private readonly ILogger<PerformanceDecorator<TRequest, TResponse>> _logger;
    private readonly Stopwatch _stopwatch;

    public PerformanceDecorator(ILogger<PerformanceDecorator<TRequest, TResponse>>  logger)
    {
        _logger = logger;
        _stopwatch = new Stopwatch();
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        
        _stopwatch.Start();

        try
        {
            return await next(cancellationToken);
        }
        finally
        {
            _stopwatch.Stop();
            var elapsedMilliseconds = _stopwatch.ElapsedMilliseconds;
            
            if (elapsedMilliseconds > 500)
                _logger.LogInformation("Request {Name} took {ElapsedMilliseconds}ms to complete", requestName, elapsedMilliseconds);
        }
    }
}
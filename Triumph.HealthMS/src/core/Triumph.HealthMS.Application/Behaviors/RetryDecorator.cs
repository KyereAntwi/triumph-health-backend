namespace Triumph.HealthMS.Application.Behaviors;

public class RetryDecorator<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest: ICommand<TResponse>
{
    private readonly ILogger<RetryDecorator<TRequest, TResponse>> _logger;

    public RetryDecorator(ILogger<RetryDecorator<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }
    
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        const int maxRetries = 3;
        var baseDelay = TimeSpan.FromSeconds(100);

        for (var attempt = 1; attempt  <= maxRetries; attempt ++)
        {
            try
            {
                if (attempt > 1)
                    _logger.LogWarning("Retrying {RequestName}, attempt {Attempt} of {MaxRetries}.", requestName, attempt, maxRetries);
                
                return await next(cancellationToken);
            }
            catch (Exception ex) when (ShouldRetry(ex, attempt, maxRetries))
            {
                var delay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                _logger.LogError(ex, "Transient error on attempt {Attempt} for {RequestName}. Retrying in {Delay}ms.", attempt, requestName, delay.TotalMilliseconds);
                await Task.Delay(delay, cancellationToken);
            }
        }

        throw new InvalidOperationException($"Failed to process {requestName} after {maxRetries} attempts.");
    }
    
    private static bool ShouldRetry(Exception ex, int attempt, int maxRetries)
    {
        if (attempt > maxRetries) return false;

        return ex is TimeoutException
                   or TaskCanceledException
                   or HttpRequestException
               || (ex is PostgresException pgEx && IsTransientPostgresError(pgEx))
               || (ex is NpgsqlException npgEx && IsTransientNpgsqlException(npgEx));
    }
    private static bool IsTransientPostgresError(PostgresException ex)
    {
        // Common transient Postgres SQLSTATE codes
        // 40001 = serialization_failure
        // 40P01 = deadlock_detected
        // 55P03 = lock_not_available
        // 53300 = too_many_connections
        // 57P01 = admin_shutdown
        // 08006 / 08003 = connection failure / does not exist
        string[] transientSqlStates =
        [
            "40001",
            "40P01",
            "55P03",
            "53300",
            "57P01",
            "08006",
            "08003"
        ];

        return !string.IsNullOrEmpty(ex.SqlState) && transientSqlStates.Contains(ex.SqlState);
    }
    private static bool IsTransientNpgsqlException(NpgsqlException ex)
    {
        // Treat Npgsql exceptions with network/socket or timeout inner exceptions as transient.
        var inner = ex.InnerException;
        return inner is SocketException or TimeoutException;
    }
}
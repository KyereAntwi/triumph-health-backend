namespace Triumph.HealthMS.Persistence.Data;

public class AuditingInterceptor : SaveChangesInterceptor
{
    private readonly ILogger<AuditingInterceptor> _logger;
    private readonly ITenantContext _tenantContext;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IDocumentSession _dbSession;

    public AuditingInterceptor(
        ILogger<AuditingInterceptor> logger, 
        ITenantContext tenantContext, 
        IPublishEndpoint publishEndpoint,
        IDocumentSession dbSession)
    {
        _logger = logger;
        _tenantContext = tenantContext;
        _publishEndpoint = publishEndpoint;
        _dbSession = dbSession;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context == null) return await base.SavingChangesAsync(eventData, result, cancellationToken);
        
        var entries = context.ChangeTracker
            .Entries()
            .Where(e => e is { Entity: TenantEntity, State: EntityState.Added or EntityState.Modified or EntityState.Deleted })
            .ToList();
        
        if (entries.Count == 0)
            return await base.SavingChangesAsync(eventData, result, cancellationToken);

        foreach (var entry in entries)
        {
            var audit = new AuditLog
            {
                TenantId = _tenantContext.TenantId,
                FacilityId = _tenantContext.FacilityId,
                UserId = _tenantContext.UserId,
                Action = entry.State.ToString(),
                EntityName = entry.Entity.GetType().Name,
                EntityId = (Guid)entry.CurrentValues["Id"]!,
                Before = entry.State == EntityState.Added ? null : entry.OriginalValues.ToObject(),
                After = entry.State == EntityState.Deleted ? null : entry.CurrentValues.ToObject(),
                TraceId = Activity.Current?.TraceId.ToString()
            };
            
            _dbSession.Store(audit);
        }
        
        await _dbSession.SaveChangesAsync(cancellationToken);
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async Task SaveChangesFailedAsync(DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context == null) return;

        var entries = context.ChangeTracker.Entries();

        foreach (var entry in entries)
        {
            _logger.LogError(
                eventData.Exception, 
                "An error occurred while saving changes to the database. TraceId: {TraceId}. Payload: {Data}", 
                Activity.Current?.TraceId, 
                entry.OriginalValues.ToObject());
        }
        await base.SaveChangesFailedAsync(eventData, cancellationToken);
    }
}
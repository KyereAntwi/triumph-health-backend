namespace Triumph.HealthMS.Application.Interfaces;

public interface IPermissionsServices
{
    Task<bool> HasPermission(PermissionValue permission, CancellationToken cancellationToken = default);
}
namespace Triumph.HealthMS.Commands.Utilities;

public static class V1Routes
{
    private const string Base = "/api/v{version:apiVersion}/";
    
    // for debug purposes
    public static string Debug => $"{Base}debug";
}
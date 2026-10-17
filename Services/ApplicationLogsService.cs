using System.ComponentModel;

namespace LocalRagDemo.Services;

public sealed class ApplicationLogsService
{
    [Description(
        "Returns recent Product API application errors " +
        "for troubleshooting and diagnostics.")]
    public string GetRecentErrors()
    {
        Console.WriteLine("[Tool] GetRecentErrors");

        return """
            Recent Product API Errors:

            [10:42:15] HTTP 401 Unauthorized
            JWT validation failed: Invalid issuer.

            [10:43:02] HTTP 401 Unauthorized
            Token issuer does not match configured authority.

            [10:44:18] HTTP 401 Unauthorized
            Authentication failed for /api/products.
            """;
    }
}
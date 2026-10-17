using System.ComponentModel;

namespace LocalRagDemo.Services;

public sealed class DeploymentService
{
    [Description(
        "Returns the current deployment status of the Product API, " +
        "including environment, version and running replicas.")]
    public string GetDeploymentStatus()
    {
        Console.WriteLine("[Tool] GetDeploymentStatus");

        return """
            Product API Deployment Status:
            Environment: Production
            Version: 2.1.0
            Platform: Kubernetes
            Status: Running
            Replicas: 3/3
            Last Deployment: 2026-10-15 10:30 UTC
            """;
    }
}
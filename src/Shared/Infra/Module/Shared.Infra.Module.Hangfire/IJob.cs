using Hangfire;

namespace Shared.Infra.Module.Hangfire;

/// <summary>
/// Contrato para jobs executáveis pelo Hangfire e expostos como endpoints (GET /jobs/{name}).
/// </summary>
public interface IJob
{
    string Name { get; }

    Task ExecuteHangFireAsync(IJobCancellationToken cancellationToken);
    Task ExecuteAsync(CancellationToken cancellationToken);
}

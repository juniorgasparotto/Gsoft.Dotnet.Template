using Hangfire;
using Shared.Core.Attributes;
using Shared.Infra.Module.Hangfire;

namespace ToDoSystem.Worker.Jobs.Jobs;

/// <summary>
/// Job de exemplo. Troque por jobs reais (ex: sincronização, notificações, limpeza).
/// </summary>
[InjectAsSingleton]
public class TestJob : IJob
{
    public string Name => "test";

    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task ExecuteHangFireAsync(IJobCancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

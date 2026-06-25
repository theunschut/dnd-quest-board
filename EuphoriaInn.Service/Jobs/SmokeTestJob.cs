using EuphoriaInn.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EuphoriaInn.Service.Jobs;

public class SmokeTestJob(
    IServiceScopeFactory scopeFactory,
    ILogger<SmokeTestJob> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        logger.LogInformation(
            "Smoke test: IEmailService resolved successfully. Type: {Type}",
            emailService.GetType().Name);
    }
}

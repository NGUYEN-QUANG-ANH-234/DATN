using HRM.backend.src.HRM.Application.DTOs.Events;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using MediatR;

namespace HRM.backend.src.HRM.Infrastructure.ExternalServices
{
    public class CentralSlaWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CentralSlaWorker> _logger;

        public CentralSlaWorker(IServiceProvider serviceProvider, ILogger<CentralSlaWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var slaRepo = scope.ServiceProvider.GetRequiredService<IBaseRepository<SlaTrackingTask>>();
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                    var violatedTasks = await slaRepo.FindAsync(t =>
                        t.Status == SlaTaskStatus.Pending &&
                        t.Deadline < DateTime.UtcNow, stoppingToken);

                    if (violatedTasks.Any())
                    {
                        foreach (var task in violatedTasks)
                        {
                            task.Status = SlaTaskStatus.Violated;
                            await slaRepo.UpdateAsync(task, stoppingToken);

                            await mediator.Publish(new SlaViolatedEvent
                            {
                                ModuleType = task.ModuleType,
                                ReferenceId = task.ReferenceId
                            }, stoppingToken);
                        }

                        await unitOfWork.CommitAsync(stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Central SLA worker cycle failed.");
                }

                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }
}

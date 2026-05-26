using HRM.backend.src.HRM.Core.Entities.TasksTraining;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TasksTraining;
using TaskStatus = HRM.backend.src.HRM.Core.Enums.TaskStatus;

namespace HRM.backend.src.HRM.Infrastructure.ExternalServices
{
    public class TaskSlaWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public TaskSlaWorker(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _serviceProvider.CreateScope();
                var taskRepo = scope.ServiceProvider.GetRequiredService<ITaskRepository>();
                var penaltyRepo = scope.ServiceProvider.GetRequiredService<IPenaltyRecordRepository>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var tasks = await taskRepo.FetchSlaViolationsAsync(DateTime.UtcNow, stoppingToken);
                foreach (var task in tasks)
                {
                    if ((task.Status == TaskStatus.Assigned || task.Status == TaskStatus.InProgress || task.Status == TaskStatus.ReworkRequired) &&
                        task.AssignedTo.HasValue)
                    {
                        task.Status = TaskStatus.Overdue;
                        await AddPenaltyIfNeededAsync(
                            penaltyRepo,
                            PenaltySourceType.Task,
                            task.Id,
                            "TASK_SUBMISSION_OVERDUE",
                            task.AssignedTo.Value,
                            ResolvePeriod(task.Deadline ?? DateTime.UtcNow),
                            1,
                            "Task submission deadline was missed.",
                            stoppingToken);
                    }
                    else if (task.Status == TaskStatus.PendingReview)
                    {
                        task.Status = TaskStatus.AutoApproved;
                        task.ApprovedAt = DateTime.UtcNow;
                        task.ProgressPercent = 100;

                        var managerId = task.Department?.ManagerId ?? task.Assignee?.Department?.ManagerId;
                        if (managerId.HasValue)
                        {
                            await AddPenaltyIfNeededAsync(
                                penaltyRepo,
                                PenaltySourceType.SLA,
                                task.Id,
                                "TASK_REVIEW_SLA_VIOLATION",
                                managerId.Value,
                                ResolvePeriod(task.ReviewDeadline ?? DateTime.UtcNow),
                                1,
                                "Manager review SLA was missed.",
                                stoppingToken);
                        }
                    }

                    taskRepo.Update(task);
                }

                if (tasks.Any())
                    await unitOfWork.CommitAsync(stoppingToken);

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private static async Task AddPenaltyIfNeededAsync(
            IPenaltyRecordRepository penaltyRepo,
            PenaltySourceType sourceType,
            int referenceId,
            string ruleCode,
            int employeeId,
            string period,
            decimal point,
            string reason,
            CancellationToken ct)
        {
            if (await penaltyRepo.ExistsForReferenceAsync(sourceType, referenceId, ruleCode, ct))
                return;

            await penaltyRepo.AddAsync(new PenaltyRecord
            {
                EmployeeId = employeeId,
                Period = period,
                SourceType = sourceType,
                ReferenceId = referenceId,
                RuleCode = ruleCode,
                PenaltyPoint = point,
                Reason = reason,
                CreatedBySystem = true,
                CreatedAt = DateTime.UtcNow
            }, ct);
        }

        private static string ResolvePeriod(DateTime date) => $"{date.Month:D2}/{date.Year}";
    }
}

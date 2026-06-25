using HRM.backend.src.HRM.Core.Entities.WorkflowRequests;
using HRM.backend.src.HRM.Core.Entities.TasksTraining;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TasksTraining;

namespace HRM.backend.src.HRM.Infrastructure.ExternalServices
{
    public class TrainingSlaWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TrainingSlaWorker> _logger;

        public TrainingSlaWorker(IServiceProvider serviceProvider, ILogger<TrainingSlaWorker> logger)
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
                    var trainingRepo = scope.ServiceProvider.GetRequiredService<ITrainingRepository>();
                    var employeeRepo = scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();
                    var penaltyRepo = scope.ServiceProvider.GetRequiredService<IPenaltyRecordRepository>();
                    var requestRepo = scope.ServiceProvider.GetRequiredService<IBaseRepository<Request>>();
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                    var trainings = await trainingRepo.FetchOverdueEvaluationsAsync(DateTime.UtcNow, stoppingToken);
                    foreach (var training in trainings)
                    {
                        training.Status = TrainingStatus.AutoCompleted;
                        training.IsPassed = true;
                        training.EvaluatedAt = DateTime.UtcNow;
                        training.CompletedAt = training.CompletedAt ?? DateTime.UtcNow;
                        training.ManagerEvaluation = string.IsNullOrWhiteSpace(training.ManagerEvaluation)
                            ? "Auto completed because manager evaluation SLA was missed."
                            : training.ManagerEvaluation;

                        var employee = training.Employee ?? await employeeRepo.GetProfileByIdAsync(training.EmployeeId, stoppingToken);
                        if (employee != null)
                        {
                            employee.Status = EmployeeStatus.Official;
                            if (employee.Type == EmployeeType.Intern || employee.Type == EmployeeType.Probation)
                                employee.Type = EmployeeType.Official;
                            employeeRepo.Update(employee);
                        }

                        if (training.ManagerId.HasValue)
                        {
                            await AddPenaltyIfNeededAsync(
                                penaltyRepo,
                                training.Id,
                                training.ManagerId.Value,
                                ResolvePeriod(training.EvaluationDeadline ?? DateTime.UtcNow),
                                stoppingToken);
                        }

                        if (!training.PromotionRequestId.HasValue)
                        {
                            var request = new Request
                            {
                                EmployeeId = training.EmployeeId,
                                RequestType = RequestType.Promotion,
                                Content = $"Training auto completed by SLA. Course: {training.CourseName}.",
                                Status = RequestStatus.Pending_HR,
                                DeadlineAt = DateTime.UtcNow.AddDays(3)
                            };

                            await requestRepo.AddAsync(request, stoppingToken);
                            await unitOfWork.CommitAsync(stoppingToken);
                            training.PromotionRequestId = request.Id;
                        }

                        trainingRepo.Update(training);
                    }

                    if (trainings.Any())
                        await unitOfWork.CommitAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Training SLA worker cycle failed.");
                }

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private static async Task AddPenaltyIfNeededAsync(
            IPenaltyRecordRepository penaltyRepo,
            int trainingId,
            int managerEmployeeId,
            string period,
            CancellationToken ct)
        {
            const string ruleCode = "TRAINING_EVAL_SLA_VIOLATION";
            if (await penaltyRepo.ExistsForReferenceAsync(PenaltySourceType.SLA, trainingId, ruleCode, ct))
                return;

            await penaltyRepo.AddAsync(new PenaltyRecord
            {
                EmployeeId = managerEmployeeId,
                Period = period,
                SourceType = PenaltySourceType.SLA,
                ReferenceId = trainingId,
                RuleCode = ruleCode,
                PenaltyPoint = 1,
                Reason = "Training evaluation SLA was missed.",
                Status = PenaltyRecordStatus.Approved,
                OccurredAt = DateTime.UtcNow,
                ViolationType = ViolationType.TrainingEvaluationSla,
                Severity = PenaltySeverity.Low,
                AffectsAttendance = false,
                AffectsPerformance = true,
                AffectsPersonnelDecision = false,
                CreatedBySystem = true,
                CreatedAt = DateTime.UtcNow
            }, ct);
        }

        private static string ResolvePeriod(DateTime date) => $"{date.Month:D2}/{date.Year}";
    }
}

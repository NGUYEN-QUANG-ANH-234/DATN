using HRM.backend.src.HRM.Core.Entities.RequestHandover;
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

        public TrainingSlaWorker(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
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

                    await requestRepo.AddAsync(new Request
                    {
                        EmployeeId = training.EmployeeId,
                        RequestType = RequestType.Promotion,
                        Content = $"Training auto completed by SLA. Course: {training.CourseName}.",
                        Status = RequestStatus.Pending_HR,
                        DeadlineAt = DateTime.UtcNow.AddDays(3)
                    }, stoppingToken);

                    trainingRepo.Update(training);
                }

                if (trainings.Any())
                    await unitOfWork.CommitAsync(stoppingToken);

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
                CreatedBySystem = true,
                CreatedAt = DateTime.UtcNow
            }, ct);
        }

        private static string ResolvePeriod(DateTime date) => $"{date.Month:D2}/{date.Year}";
    }
}

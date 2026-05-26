using HRM.backend.src.HRM.Application.DTOs.TasksTraining;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.TasksTraining.Usecases;
using HRM.backend.src.HRM.Core.Entities.RequestHandover;
using HRM.backend.src.HRM.Core.Entities.TasksTraining;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TasksTraining;
using TaskStatus = HRM.backend.src.HRM.Core.Enums.TaskStatus;

namespace HRM.backend.src.HRM.Application.UseCases.TasksTraining
{
    public class TrainingUseCase : ITrainingUseCase
    {
        private readonly ITrainingRepository _trainingRepo;
        private readonly ITaskRepository _taskRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IBaseRepository<Request> _requestRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILockService _lockService;

        public TrainingUseCase(
            ITrainingRepository trainingRepo,
            ITaskRepository taskRepo,
            IEmployeeRepository employeeRepo,
            IBaseRepository<Request> requestRepo,
            IUnitOfWork unitOfWork,
            ILockService lockService)
        {
            _trainingRepo = trainingRepo;
            _taskRepo = taskRepo;
            _employeeRepo = employeeRepo;
            _requestRepo = requestRepo;
            _unitOfWork = unitOfWork;
            _lockService = lockService;
        }

        public async Task<TrainingSummaryDto> GetTrainingReportAsync(int trainingId, int actorAccountId, string role, CancellationToken ct = default)
        {
            var training = await _trainingRepo.GetByIdAsync(trainingId, ct)
                ?? throw new InvalidOperationException("Training record not found.");
            await EnsureCanEvaluateAsync(training, actorAccountId, role, ct);
            var tasks = await _taskRepo.GetByTrainingAsync(trainingId, ct);
            return MapTraining(training, tasks);
        }

        public async Task<List<TrainingSummaryDto>> GetPendingEvaluationsAsync(int actorAccountId, string role, CancellationToken ct = default)
        {
            var manager = await _employeeRepo.GetByAccountIdAsync(actorAccountId, ct)
                ?? throw new UnauthorizedAccessException("Account is not linked to an employee profile.");
            if (!IsManager(role) && !IsHrOrAdmin(role))
                throw new UnauthorizedAccessException("Only Manager, HR or Admin can evaluate training.");

            var trainings = IsManager(role)
                ? await _trainingRepo.GetPendingEvaluationByManagerAsync(manager.Id, ct)
                : await _trainingRepo.GetByStatusAsync(TrainingStatus.PendingEvaluation, ct);
            return trainings.Select(t => MapTraining(t, t.Tasks.ToList())).ToList();
        }

        public async Task EvaluateProcessAsync(EvaluateTrainingDto dto, int actorAccountId, string role, CancellationToken ct = default)
        {
            await _lockService.GetWithLockAsync($"training_evaluate_{dto.TrainingId}", async innerCt =>
            {
                var training = await _trainingRepo.GetByIdAsync(dto.TrainingId, innerCt)
                    ?? throw new InvalidOperationException("Training record not found.");
                await EnsureCanEvaluateAsync(training, actorAccountId, role, innerCt);

                if (!dto.IsApproved)
                {
                    training.Status = TrainingStatus.Extended;
                    training.ManagerEvaluation = dto.ManagerEvaluation;
                    training.FinalScore = dto.FinalScore;
                    training.EvaluationDeadline = DateTime.UtcNow.AddDays(7);
                    _trainingRepo.Update(training);
                    await _unitOfWork.CommitAsync(innerCt);
                    return true;
                }

                var tasks = await _taskRepo.GetByTrainingAsync(training.Id, innerCt);
                if (tasks.Any(t => t.Status != TaskStatus.Completed && t.Status != TaskStatus.AutoApproved))
                    throw new InvalidOperationException("All training tasks must be completed before approving the training process.");

                training.Status = TrainingStatus.Completed;
                training.IsPassed = true;
                training.FinalScore = dto.FinalScore;
                training.ManagerEvaluation = dto.ManagerEvaluation;
                training.CompletedAt = training.CompletedAt ?? DateTime.UtcNow;
                training.EvaluatedAt = DateTime.UtcNow;

                var employee = training.Employee ?? await _employeeRepo.GetProfileByIdAsync(training.EmployeeId, innerCt);
                if (employee != null)
                {
                    employee.Status = EmployeeStatus.Official;
                    if (employee.Type == EmployeeType.Intern || employee.Type == EmployeeType.Probation)
                        employee.Type = EmployeeType.Official;
                    _employeeRepo.Update(employee);
                }

                if (dto.CreatePromotionRequest)
                {
                    var request = new Request
                    {
                        EmployeeId = training.EmployeeId,
                        RequestType = RequestType.Promotion,
                        Content = $"Training completed. Course: {training.CourseName}. Score: {dto.FinalScore?.ToString() ?? "N/A"}.",
                        Status = RequestStatus.Pending_HR,
                        DeadlineAt = DateTime.UtcNow.AddDays(3)
                    };
                    await _requestRepo.AddAsync(request, innerCt);
                }

                _trainingRepo.Update(training);
                await _unitOfWork.CommitAsync(innerCt);
                return true;
            }, cancellationToken: ct);
        }

        private async Task EnsureCanEvaluateAsync(Training training, int actorAccountId, string role, CancellationToken ct)
        {
            if (IsHrOrAdmin(role))
                return;
            if (!IsManager(role))
                throw new UnauthorizedAccessException("Only Manager, HR or Admin can evaluate training.");

            var manager = await _employeeRepo.GetByAccountIdAsync(actorAccountId, ct)
                ?? throw new UnauthorizedAccessException("Account is not linked to an employee profile.");
            if (training.ManagerId.HasValue && training.ManagerId == manager.Id)
                return;
            if (training.DeptId.HasValue && training.DeptId == manager.DeptId)
                return;
            throw new UnauthorizedAccessException("Manager can only evaluate training in their department.");
        }

        private static TrainingSummaryDto MapTraining(Training training, List<WorkTask> tasks)
        {
            return new TrainingSummaryDto
            {
                Id = training.Id,
                EmployeeId = training.EmployeeId,
                EmployeeName = training.Employee?.FullName ?? string.Empty,
                DepartmentName = training.Department?.DeptName ?? training.Employee?.Department?.DeptName,
                CourseName = training.CourseName,
                TrainingType = training.TrainingType,
                Status = training.Status.ToString(),
                FinalScore = training.FinalScore,
                ManagerEvaluation = training.ManagerEvaluation,
                IsPassed = training.IsPassed,
                EvaluationDeadline = training.EvaluationDeadline,
                Tasks = tasks.Select(t => new TaskResponseDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    TaskType = t.TaskType.ToString(),
                    EmployeeId = t.AssignedTo,
                    EmployeeName = t.Assignee?.FullName,
                    DepartmentName = t.Department?.DeptName ?? t.Assignee?.Department?.DeptName,
                    ProgressPercent = t.ProgressPercent,
                    Status = t.Status.ToString(),
                    EvidencePath = t.EvidencePath,
                    Deadline = t.Deadline,
                    ReviewDeadline = t.ReviewDeadline,
                    SubmittedAt = t.SubmittedAt,
                    ApprovedAt = t.ApprovedAt
                }).ToList()
            };
        }

        private static bool IsManager(string role) =>
            role.Equals("Manager", StringComparison.OrdinalIgnoreCase) ||
            role.Equals("Truong phong", StringComparison.OrdinalIgnoreCase);

        private static bool IsHrOrAdmin(string role) =>
            role.Equals("HR", StringComparison.OrdinalIgnoreCase) ||
            role.Equals("Admin", StringComparison.OrdinalIgnoreCase);
    }
}

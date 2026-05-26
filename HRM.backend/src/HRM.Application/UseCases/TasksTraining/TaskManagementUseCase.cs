using HRM.backend.src.HRM.Application.DTOs.TasksTraining;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.TasksTraining.Usecases;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.TasksTraining;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TasksTraining;
using TaskStatus = HRM.backend.src.HRM.Core.Enums.TaskStatus;

namespace HRM.backend.src.HRM.Application.UseCases.TasksTraining
{
    public class TaskManagementUseCase : ITaskManagementUseCase
    {
        private readonly ITaskRepository _taskRepo;
        private readonly ITaskProgressRepository _progressRepo;
        private readonly ITaskFeedbackRepository _feedbackRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IStorageService _storageService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILockService _lockService;

        public TaskManagementUseCase(
            ITaskRepository taskRepo,
            ITaskProgressRepository progressRepo,
            ITaskFeedbackRepository feedbackRepo,
            IEmployeeRepository employeeRepo,
            IStorageService storageService,
            IUnitOfWork unitOfWork,
            ILockService lockService)
        {
            _taskRepo = taskRepo;
            _progressRepo = progressRepo;
            _feedbackRepo = feedbackRepo;
            _employeeRepo = employeeRepo;
            _storageService = storageService;
            _unitOfWork = unitOfWork;
            _lockService = lockService;
        }

        public async Task<List<TaskResponseDto>> GetMyTasksAsync(int actorAccountId, CancellationToken ct = default)
        {
            var employee = await _employeeRepo.GetByAccountIdAsync(actorAccountId, ct)
                ?? throw new UnauthorizedAccessException("Account is not linked to an employee profile.");
            var tasks = await _taskRepo.GetByAssigneeAsync(employee.Id, ct);
            return tasks.Select(MapTask).ToList();
        }

        public async Task<List<TaskResponseDto>> GetPendingReviewAsync(int actorAccountId, string role, CancellationToken ct = default)
        {
            var manager = await _employeeRepo.GetByAccountIdAsync(actorAccountId, ct)
                ?? throw new UnauthorizedAccessException("Account is not linked to an employee profile.");
            if (!IsManager(role) && !IsHrOrAdmin(role))
                throw new UnauthorizedAccessException("Only Manager, HR or Admin can review tasks.");
            if (manager.DeptId == null && IsManager(role))
                throw new UnauthorizedAccessException("Manager account has no department.");

            var tasks = IsManager(role)
                ? await _taskRepo.GetPendingReviewByDeptAsync(manager.DeptId!.Value, ct)
                : await _taskRepo.GetByStatusAsync(TaskStatus.PendingReview, ct);
            return tasks.Select(MapTask).ToList();
        }

        public async Task<TaskResponseDto> GetReviewContextAsync(int id, int actorAccountId, string role, CancellationToken ct = default)
        {
            var task = await _taskRepo.GetByIdAsync(id, ct)
                ?? throw new InvalidOperationException("Task not found.");
            await EnsureCanViewOrReviewAsync(task, actorAccountId, role, ct);
            return MapTask(task);
        }

        public async Task UpdateProgressAsync(int id, TaskProgressUpdateDto dto, int actorAccountId, CancellationToken ct = default)
        {
            var employee = await _employeeRepo.GetByAccountIdAsync(actorAccountId, ct)
                ?? throw new UnauthorizedAccessException("Account is not linked to an employee profile.");

            await _lockService.GetWithLockAsync($"task_progress_{id}", async innerCt =>
            {
                var task = await _taskRepo.GetByIdAsync(id, innerCt)
                    ?? throw new InvalidOperationException("Task not found.");
                if (task.AssignedTo != employee.Id)
                    throw new UnauthorizedAccessException("Only the assigned employee can update this task.");
                if (task.Status != TaskStatus.Assigned &&
                    task.Status != TaskStatus.InProgress &&
                    task.Status != TaskStatus.ReworkRequired)
                    throw new InvalidOperationException("Task is not open for progress update.");

                var progressPercent = Math.Clamp(dto.ProgressPercent, 0, 100);
                var evidencePath = dto.EvidenceFile != null
                    ? await _storageService.UploadFileAsync(dto.EvidenceFile, "task-evidence", innerCt)
                    : task.EvidencePath;

                await _progressRepo.AddAsync(new TaskProgress
                {
                    TaskId = task.Id,
                    EmployeeId = employee.Id,
                    ProgressPercent = progressPercent,
                    Note = dto.Note,
                    EvidencePath = evidencePath,
                    SubmittedAt = DateTime.UtcNow
                }, innerCt);

                task.ProgressPercent = progressPercent;
                task.EvidencePath = evidencePath;
                task.SubmittedAt = DateTime.UtcNow;
                task.ReviewDeadline = DateTime.UtcNow.AddDays(2);
                task.Status = TaskStatus.PendingReview;
                _taskRepo.Update(task);

                await _unitOfWork.CommitAsync(innerCt);
                return true;
            }, cancellationToken: ct);
        }

        public async Task ProvideFeedbackAsync(int id, TaskFeedbackDto dto, int actorAccountId, string role, CancellationToken ct = default)
        {
            await ReviewTaskAsync(id, actorAccountId, role, false, dto.Content, ct);
        }

        public async Task ApproveTaskAsync(int id, int actorAccountId, string role, CancellationToken ct = default)
        {
            await ReviewTaskAsync(id, actorAccountId, role, true, "Task approved.", ct);
        }

        private async Task ReviewTaskAsync(int id, int actorAccountId, string role, bool approved, string? note, CancellationToken ct)
        {
            var reviewer = await _employeeRepo.GetByAccountIdAsync(actorAccountId, ct)
                ?? throw new UnauthorizedAccessException("Account is not linked to an employee profile.");

            await _lockService.GetWithLockAsync($"task_review_{id}", async innerCt =>
            {
                var task = await _taskRepo.GetByIdAsync(id, innerCt)
                    ?? throw new InvalidOperationException("Task not found.");
                EnsureReviewer(task, reviewer, role);

                var latestProgress = await _progressRepo.GetLatestByTaskAsync(task.Id, innerCt);
                await _feedbackRepo.AddAsync(new TaskFeedback
                {
                    TaskId = task.Id,
                    ProgressId = latestProgress?.Id,
                    ReviewerId = reviewer.Id,
                    FeedbackType = approved ? TaskFeedbackType.Approved : TaskFeedbackType.ReworkRequest,
                    Content = note,
                    CreatedAt = DateTime.UtcNow
                }, innerCt);

                task.Status = approved ? TaskStatus.Completed : TaskStatus.ReworkRequired;
                if (approved)
                {
                    task.ProgressPercent = 100;
                    task.ApprovedAt = DateTime.UtcNow;
                }
                _taskRepo.Update(task);

                await _unitOfWork.CommitAsync(innerCt);
                return true;
            }, cancellationToken: ct);
        }

        private async Task EnsureCanViewOrReviewAsync(WorkTask task, int actorAccountId, string role, CancellationToken ct)
        {
            var employee = await _employeeRepo.GetByAccountIdAsync(actorAccountId, ct)
                ?? throw new UnauthorizedAccessException("Account is not linked to an employee profile.");
            if (task.AssignedTo == employee.Id)
                return;
            EnsureReviewer(task, employee, role);
        }

        private static void EnsureReviewer(WorkTask task, Employee reviewer, string role)
        {
            if (IsHrOrAdmin(role))
                return;
            if (!IsManager(role))
                throw new UnauthorizedAccessException("Only Manager, HR or Admin can review tasks.");
            if (task.DeptId.HasValue && reviewer.DeptId == task.DeptId)
                return;
            throw new UnauthorizedAccessException("Manager can only review tasks in their department.");
        }

        private static TaskResponseDto MapTask(WorkTask task)
        {
            return new TaskResponseDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                TaskType = task.TaskType.ToString(),
                EmployeeId = task.AssignedTo,
                EmployeeName = task.Assignee?.FullName,
                DepartmentName = task.Department?.DeptName ?? task.Assignee?.Department?.DeptName,
                ProgressPercent = task.ProgressPercent,
                Status = task.Status.ToString(),
                EvidencePath = task.EvidencePath,
                Deadline = task.Deadline,
                ReviewDeadline = task.ReviewDeadline,
                SubmittedAt = task.SubmittedAt,
                ApprovedAt = task.ApprovedAt,
                Progresses = task.Progresses.OrderByDescending(p => p.SubmittedAt).Select(p => new TaskProgressResponseDto
                {
                    Id = p.Id,
                    ProgressPercent = p.ProgressPercent,
                    Note = p.Note,
                    EvidencePath = p.EvidencePath,
                    SubmittedAt = p.SubmittedAt
                }).ToList(),
                Feedbacks = task.Feedbacks.OrderByDescending(f => f.CreatedAt).Select(f => new TaskFeedbackResponseDto
                {
                    Id = f.Id,
                    FeedbackType = f.FeedbackType.ToString(),
                    Content = f.Content,
                    ReviewerName = f.Reviewer?.FullName,
                    CreatedAt = f.CreatedAt
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

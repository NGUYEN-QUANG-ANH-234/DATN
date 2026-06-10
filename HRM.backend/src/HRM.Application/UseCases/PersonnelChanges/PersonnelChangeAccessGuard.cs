using HRM.backend.src.HRM.Application.Interfaces.PersonnelChanges.Services;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.Organization;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.Organization;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TasksTraining;

namespace HRM.backend.src.HRM.Application.UseCases.PersonnelChanges
{
    public class PersonnelChangeAccessGuard : IPersonnelChangeAccessGuard
    {
        private const string PersonnelChangeEvidencePrefix = "/uploads/personnel-change-evidence/";

        private readonly IAccountRepository _accountRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IDepartmentRepository _departmentRepo;
        private readonly IPositionRepository _positionRepo;
        private readonly IBaseRepository<JobLevel> _jobLevelRepo;
        private readonly IContractRepository _contractRepo;
        private readonly IPerformanceReviewRepository _performanceReviewRepo;
        private readonly IPenaltyRecordRepository _penaltyRecordRepo;

        public PersonnelChangeAccessGuard(
            IAccountRepository accountRepo,
            IEmployeeRepository employeeRepo,
            IDepartmentRepository departmentRepo,
            IPositionRepository positionRepo,
            IBaseRepository<JobLevel> jobLevelRepo,
            IContractRepository contractRepo,
            IPerformanceReviewRepository performanceReviewRepo,
            IPenaltyRecordRepository penaltyRecordRepo)
        {
            _accountRepo = accountRepo;
            _employeeRepo = employeeRepo;
            _departmentRepo = departmentRepo;
            _positionRepo = positionRepo;
            _jobLevelRepo = jobLevelRepo;
            _contractRepo = contractRepo;
            _performanceReviewRepo = performanceReviewRepo;
            _penaltyRecordRepo = penaltyRecordRepo;
        }

        public async Task<Employee> EnsureCanAccessEmployeeAsync(
            int employeeId,
            int actorAccountId,
            CancellationToken ct,
            bool requireActive = true)
        {
            if (employeeId <= 0)
                throw new ArgumentException("Employee is required.");

            var target = await _employeeRepo.GetByIdAsync(employeeId, ct)
                ?? throw new KeyNotFoundException("Employee was not found.");

            if (requireActive && IsInactiveEmployee(target.Status))
                throw new InvalidOperationException("Selected employee is no longer active.");

            var actor = await GetActorContextAsync(actorAccountId, ct);
            if (CanViewAllEmployees(actor.RoleName))
                return target;

            if (IsManager(actor.RoleName))
            {
                if (actor.Employee?.DeptId.HasValue != true)
                    throw new UnauthorizedAccessException("Manager account is not linked to a department.");

                if (target.DeptId != actor.Employee!.DeptId)
                    throw new UnauthorizedAccessException("Manager can only select employees in their department.");

                return target;
            }

            if (actor.Employee?.Id == target.Id)
                return target;

            throw new UnauthorizedAccessException("You do not have permission to select this employee.");
        }

        public async Task EnsureActiveDepartmentAsync(int? departmentId, CancellationToken ct)
        {
            if (!departmentId.HasValue)
                return;

            if (departmentId.Value <= 0)
                throw new ArgumentException("Department is invalid.");

            var department = await _departmentRepo.GetByIdAsync(departmentId.Value, ct)
                ?? throw new KeyNotFoundException("Department was not found.");

            if (department.Status != DeptStatus.Active)
                throw new InvalidOperationException("Selected department is not active.");
        }

        public async Task EnsureActivePositionAsync(int? positionId, CancellationToken ct)
        {
            if (!positionId.HasValue)
                return;

            if (positionId.Value <= 0)
                throw new ArgumentException("Position is invalid.");

            var position = await _positionRepo.GetByIdAsync(positionId.Value, ct)
                ?? throw new KeyNotFoundException("Position was not found.");

            if (!position.IsActive)
                throw new InvalidOperationException("Selected position is not active.");
        }

        public async Task EnsureActiveJobLevelAsync(int? jobLevelId, CancellationToken ct)
        {
            if (!jobLevelId.HasValue)
                return;

            if (jobLevelId.Value <= 0)
                throw new ArgumentException("Job level is invalid.");

            var jobLevel = await _jobLevelRepo.GetByIdAsync(jobLevelId.Value, ct)
                ?? throw new KeyNotFoundException("Job level was not found.");

            if (!jobLevel.IsActive)
                throw new InvalidOperationException("Selected job level is not active.");
        }

        public Task EnsureCanUseManagerAsync(int? managerId, int actorAccountId, CancellationToken ct)
        {
            return managerId.HasValue
                ? EnsureCanAccessEmployeeAsync(managerId.Value, actorAccountId, ct)
                : Task.CompletedTask;
        }

        public async Task EnsurePerformanceReviewBelongsToEmployeeAsync(
            int? performanceReviewId,
            int employeeId,
            CancellationToken ct)
        {
            if (!performanceReviewId.HasValue)
                return;

            var review = await _performanceReviewRepo.GetDetailAsync(performanceReviewId.Value, ct)
                ?? throw new KeyNotFoundException("Performance review was not found.");

            if (review.EmployeeId != employeeId)
                throw new InvalidOperationException("Performance review does not belong to the selected employee.");
        }

        public async Task EnsurePenaltyRecordBelongsToEmployeeAsync(
            int? penaltyRecordId,
            int employeeId,
            CancellationToken ct)
        {
            if (!penaltyRecordId.HasValue)
                return;

            var penalty = await _penaltyRecordRepo.GetByIdAsync(penaltyRecordId.Value, ct)
                ?? throw new KeyNotFoundException("Penalty record was not found.");

            if (penalty.EmployeeId != employeeId)
                throw new InvalidOperationException("Penalty record does not belong to the selected employee.");
        }

        public async Task EnsureContractBelongsToEmployeeAsync(
            int? contractId,
            int employeeId,
            CancellationToken ct)
        {
            if (!contractId.HasValue)
                return;

            var contract = await _contractRepo.GetByIdAsync(contractId.Value, ct)
                ?? throw new KeyNotFoundException("Contract was not found.");

            if (contract.EmployeeId != employeeId)
                throw new InvalidOperationException("Contract does not belong to the selected employee.");
        }

        public async Task EnsurePlacementReferencesAsync(
            int? departmentId,
            int? positionId,
            int? managerId,
            int? jobLevelId,
            int actorAccountId,
            CancellationToken ct)
        {
            await EnsureActiveDepartmentAsync(departmentId, ct);
            await EnsureActivePositionAsync(positionId, ct);
            await EnsureCanUseManagerAsync(managerId, actorAccountId, ct);
            await EnsureActiveJobLevelAsync(jobLevelId, ct);
        }

        public void EnsurePersonnelChangeEvidencePath(string? evidenceFilePath)
        {
            if (string.IsNullOrWhiteSpace(evidenceFilePath))
                return;

            var normalized = evidenceFilePath.Trim().Replace('\\', '/');
            if (normalized.Contains("..", StringComparison.Ordinal))
                throw new ArgumentException("Evidence file path is invalid.");

            if (!normalized.StartsWith(PersonnelChangeEvidencePrefix, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Evidence file must be uploaded through the personnel change upload endpoint.");
        }

        private async Task<ActorContext> GetActorContextAsync(int actorAccountId, CancellationToken ct)
        {
            var account = await _accountRepo.GetByIdWithRoleAsync(actorAccountId, ct)
                ?? throw new UnauthorizedAccessException("Actor account was not found.");
            var employee = await _employeeRepo.GetByAccountIdAsync(actorAccountId, ct);

            return new ActorContext(account.Role.RoleName, employee);
        }

        private static bool CanViewAllEmployees(string roleName) =>
            IsAny(roleName, "Admin", "HR", "Director");

        private static bool IsManager(string roleName) =>
            IsAny(roleName, "Manager", "Truong phong", "Trưởng phòng");

        private static bool IsAny(string roleName, params string[] values) =>
            values.Any(value => string.Equals(roleName, value, StringComparison.OrdinalIgnoreCase));

        private static bool IsInactiveEmployee(EmployeeStatus status) =>
            status is EmployeeStatus.Resigned or EmployeeStatus.Terminated or EmployeeStatus.Dismissed;

        private sealed record ActorContext(string RoleName, Employee? Employee);
    }
}

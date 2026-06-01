using HRM.backend.src.HRM.Application.DTOs.PersonnelChanges;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.PersonnelChanges.Services;
using HRM.backend.src.HRM.Application.Interfaces.PersonnelChanges.UseCases;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.PersonnelChanges;
using HRM.backend.src.HRM.Core.Entities.RequestHandover;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.Organization;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.PersonnelChanges;

namespace HRM.backend.src.HRM.Application.UseCases.PersonnelChanges
{
    public class SeniorAppointmentUseCase : ISeniorAppointmentUseCase
    {
        private readonly IPersonnelChangeRepository _personnelChangeRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IDepartmentRepository _departmentRepo;
        private readonly IBaseRepository<EmploymentHistory> _historyRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILockService _lockService;
        private readonly PersonnelChangeRiskSummaryBuilder _riskSummaryBuilder;
        private readonly IPersonnelChangeContractFlowService _contractFlowService;
        private readonly IPersonnelChangeUseCase _personnelChangeUseCase;

        public SeniorAppointmentUseCase(
            IPersonnelChangeRepository personnelChangeRepo,
            IEmployeeRepository employeeRepo,
            IDepartmentRepository departmentRepo,
            IBaseRepository<EmploymentHistory> historyRepo,
            IUnitOfWork unitOfWork,
            ILockService lockService,
            PersonnelChangeRiskSummaryBuilder riskSummaryBuilder,
            IPersonnelChangeContractFlowService contractFlowService,
            IPersonnelChangeUseCase personnelChangeUseCase)
        {
            _personnelChangeRepo = personnelChangeRepo;
            _employeeRepo = employeeRepo;
            _departmentRepo = departmentRepo;
            _historyRepo = historyRepo;
            _unitOfWork = unitOfWork;
            _lockService = lockService;
            _riskSummaryBuilder = riskSummaryBuilder;
            _contractFlowService = contractFlowService;
            _personnelChangeUseCase = personnelChangeUseCase;
        }

        public async Task<PersonnelChangeDetailDto> CreateSeniorAppointmentAsync(
            CreateSeniorAppointmentDto dto,
            int actorAccountId,
            CancellationToken ct)
        {
            if (dto.EmployeeId <= 0)
                throw new ArgumentException("Employee is required.");
            if (dto.NewPositionId <= 0)
                throw new ArgumentException("New position is required.");

            var employee = await _employeeRepo.GetByIdAsync(dto.EmployeeId, ct)
                ?? throw new KeyNotFoundException("Employee was not found.");

            var request = new PersonnelChangeRequest
            {
                EmployeeId = employee.Id,
                ChangeType = PersonnelChangeType.SeniorAppointment,
                Status = PersonnelChangeStatus.PendingEmployeeConsent,
                RequestedByAccountId = actorAccountId,
                RequestedAt = DateTime.UtcNow,
                Reason = dto.Reason,
                EffectiveDate = dto.EffectiveDate,

                CurrentDepartmentId = employee.DeptId,
                CurrentPositionId = employee.PositionId,
                CurrentManagerId = employee.ManagerId,
                CurrentJobLevelId = employee.JobLevelId,
                CurrentEmployeeType = employee.Type,

                NewDepartmentId = dto.NewDepartmentId ?? employee.DeptId,
                NewPositionId = dto.NewPositionId,
                NewManagerId = dto.IsDepartmentManager ? employee.Id : dto.ReportsToManagerId,
                NewJobLevelId = dto.NewJobLevelId,
                NewEmployeeType = employee.Type,

                RequiresEmployeeConsent = true,
                EmployeeConsentStatus = PersonnelChangeConsentStatus.Pending,
                RequiresContractFlow = true,
                ContractFlowType = dto.ContractFlowType == PersonnelChangeContractFlowType.None
                    ? PersonnelChangeContractFlowType.ContractAddendum
                    : dto.ContractFlowType,
                RelatedContractId = dto.RelatedContractId,
                ContractFlowStatus = "NotStarted",
                RequiresDirectorApproval = false,
                RequiresHRProcessing = true,
                HRAssignedAccountId = actorAccountId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                await _personnelChangeRepo.AddAsync(request, ct);
                await _unitOfWork.CommitAsync(ct);
            }, ct);

            await AddHistoryAndSnapshotAsync(
                request.Id,
                "SeniorAppointmentCreated",
                null,
                request.Status,
                actorAccountId,
                request.Reason,
                ct);

            return await _personnelChangeUseCase.GetDetailAsync(request.Id, ct);
        }

        public Task<PersonnelChangeDetailDto> SubmitAppointmentConsentAsync(
            int id,
            int actorAccountId,
            AppointmentConsentDto dto,
            CancellationToken ct)
        {
            return MutateSeniorAppointmentAsync(id, actorAccountId, async (request, innerCt) =>
            {
                EnsureStatus(request, PersonnelChangeStatus.PendingEmployeeConsent);
                await EnsureSelectedEmployeeCanConsentAsync(request, actorAccountId, innerCt);

                var oldStatus = request.Status;
                request.EmployeeConsentAt = DateTime.UtcNow;
                request.EmployeeConsentNote = dto.Note;
                request.EmployeeConsentStatus = dto.IsAccepted
                    ? PersonnelChangeConsentStatus.Accepted
                    : PersonnelChangeConsentStatus.Declined;
                request.Status = dto.IsAccepted
                    ? PersonnelChangeStatus.PendingContractFlow
                    : PersonnelChangeStatus.EmployeeDeclined;

                if (!dto.IsAccepted)
                    request.RejectedReason = dto.Note;

                await AddHistoryAsync(request.Id, "AppointmentConsentSubmitted", oldStatus, request.Status, actorAccountId, dto.Note, innerCt);
            }, ct);
        }

        public Task<PersonnelChangeDetailDto> StartHrContractFlowAsync(
            int id,
            int actorAccountId,
            HrContractFlowDto dto,
            CancellationToken ct)
        {
            if (dto.ContractFlowType == PersonnelChangeContractFlowType.None)
                throw new ArgumentException("Contract flow type is required.");

            return MutateSeniorAppointmentAsync(id, actorAccountId, async (request, innerCt) =>
            {
                EnsureStatus(request, PersonnelChangeStatus.PendingContractFlow);

                if (request.EmployeeConsentStatus != PersonnelChangeConsentStatus.Accepted)
                    throw new InvalidOperationException("Employee consent must be accepted before starting contract flow.");
                if (request.ContractLinks.Any())
                    throw new InvalidOperationException("Contract flow has already been started.");

                var oldStatus = request.Status;
                request.RequiresContractFlow = true;
                request.ContractFlowType = dto.ContractFlowType;
                request.RelatedContractId = dto.RelatedContractId ?? request.RelatedContractId;
                request.ContractFlowStatus = "Pending";
                request.HRAssignedAccountId = actorAccountId;
                request.HRNote = dto.Note;
                request.HRProcessedAt = DateTime.UtcNow;

                await AddHistoryAsync(request.Id, "AppointmentHrContractFlowStarted", oldStatus, request.Status, actorAccountId, dto.Note, innerCt);
                await _contractFlowService.CreateContractFlowAsync(request, innerCt);
            }, ct);
        }

        public Task<PersonnelChangeDetailDto> IssueAppointmentDecisionAsync(
            int id,
            int actorAccountId,
            IssueAppointmentDecisionDto dto,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.DecisionNumber))
                throw new ArgumentException("Decision number is required.");

            return MutateSeniorAppointmentAsync(id, actorAccountId, async (request, innerCt) =>
            {
                EnsureStatus(
                    request,
                    PersonnelChangeStatus.ContractAccepted,
                    PersonnelChangeStatus.PendingDecisionIssuance,
                    PersonnelChangeStatus.ReadyToExecute);
                _contractFlowService.EnsureCanExecute(request);

                var oldStatus = request.Status;
                if (request.Status == PersonnelChangeStatus.ContractAccepted)
                {
                    await AddHistoryAsync(
                        request.Id,
                        "AppointmentPendingDecisionIssuance",
                        PersonnelChangeStatus.ContractAccepted,
                        PersonnelChangeStatus.PendingDecisionIssuance,
                        actorAccountId,
                        dto.Note,
                        innerCt);
                }

                request.DecisionNumber = dto.DecisionNumber.Trim();
                request.DecisionFilePath = dto.DecisionFilePath;
                request.DecisionIssuedAt = dto.DecisionIssuedAt ?? DateTime.UtcNow;
                request.Status = PersonnelChangeStatus.ReadyToExecute;

                await AddHistoryAsync(request.Id, "AppointmentDecisionIssued", oldStatus, request.Status, actorAccountId, dto.Note, innerCt);
            }, ct);
        }

        public Task<PersonnelChangeDetailDto> ExecuteSeniorAppointmentAsync(
            int id,
            int actorAccountId,
            ExecutePersonnelChangeDto dto,
            CancellationToken ct)
        {
            return MutateSeniorAppointmentAsync(id, actorAccountId, async (request, innerCt) =>
            {
                EnsureStatus(request, PersonnelChangeStatus.ReadyToExecute);
                _contractFlowService.EnsureCanExecute(request);

                if (!request.EmployeeId.HasValue)
                    throw new InvalidOperationException("Senior appointment requires an employee before execution.");

                var employee = await _employeeRepo.GetByIdAsync(request.EmployeeId.Value, innerCt)
                    ?? throw new KeyNotFoundException("Employee was not found.");

                await ApplyAppointmentAsync(request, employee, innerCt);

                var oldStatus = request.Status;
                request.Status = PersonnelChangeStatus.Completed;
                request.CompletedAt = dto.CompletedAt ?? DateTime.UtcNow;

                await AddHistoryAsync(request.Id, "SeniorAppointmentExecuted", oldStatus, request.Status, actorAccountId, dto.Note, innerCt);
            }, ct);
        }

        private async Task<PersonnelChangeDetailDto> MutateSeniorAppointmentAsync(
            int id,
            int actorAccountId,
            Func<PersonnelChangeRequest, CancellationToken, Task> mutation,
            CancellationToken ct)
        {
            await _lockService.GetWithLockAsync($"personnel_change_{id}", async innerCt =>
            {
                await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                    var request = await _personnelChangeRepo.GetDetailAsync(id, innerCt)
                        ?? throw new KeyNotFoundException("Personnel change request was not found.");

                    EnsureSeniorAppointment(request);
                    await mutation(request, innerCt);

                    request.UpdatedAt = DateTime.UtcNow;
                    _personnelChangeRepo.Update(request);
                    await _unitOfWork.CommitAsync(innerCt);
                }, innerCt);

                return true;
            }, cancellationToken: ct);

            await SaveSnapshotAsync(id, actorAccountId, ct);
            return await _personnelChangeUseCase.GetDetailAsync(id, ct);
        }

        private async Task ApplyAppointmentAsync(PersonnelChangeRequest request, Employee employee, CancellationToken ct)
        {
            var effectiveDate = request.EffectiveDate ?? DateTime.UtcNow.Date;

            if (request.NewDepartmentId.HasValue && request.NewDepartmentId != employee.DeptId)
            {
                await AddEmploymentHistoryAsync(
                    employee.Id,
                    HistoryType.Transfer,
                    $"DeptId: {employee.DeptId?.ToString() ?? "null"}",
                    $"DeptId: {request.NewDepartmentId.Value}",
                    effectiveDate,
                    ct);
                employee.DeptId = request.NewDepartmentId.Value;
            }

            if (request.NewPositionId.HasValue && request.NewPositionId != employee.PositionId)
            {
                await AddEmploymentHistoryAsync(
                    employee.Id,
                    HistoryType.Appointment,
                    $"PositionId: {employee.PositionId?.ToString() ?? "null"}",
                    $"PositionId: {request.NewPositionId.Value}",
                    effectiveDate,
                    ct);
                employee.PositionId = request.NewPositionId.Value;
            }

            if (request.NewJobLevelId.HasValue && request.NewJobLevelId != employee.JobLevelId)
            {
                await AddEmploymentHistoryAsync(
                    employee.Id,
                    HistoryType.Appointment,
                    $"JobLevelId: {employee.JobLevelId?.ToString() ?? "null"}",
                    $"JobLevelId: {request.NewJobLevelId.Value}",
                    effectiveDate,
                    ct);
                employee.JobLevelId = request.NewJobLevelId.Value;
            }

            if (request.NewManagerId.HasValue && request.NewManagerId.Value != employee.Id)
            {
                await AddEmploymentHistoryAsync(
                    employee.Id,
                    HistoryType.Appointment,
                    $"ManagerId: {employee.ManagerId?.ToString() ?? "null"}",
                    $"ManagerId: {request.NewManagerId.Value}",
                    effectiveDate,
                    ct);
                employee.ManagerId = request.NewManagerId.Value;
            }

            if (request.NewManagerId == employee.Id)
                await ApplyDepartmentManagerAsync(request, employee.Id, effectiveDate, ct);

            _employeeRepo.Update(employee);
        }

        private async Task ApplyDepartmentManagerAsync(
            PersonnelChangeRequest request,
            int employeeId,
            DateTime effectiveDate,
            CancellationToken ct)
        {
            var departmentId = request.NewDepartmentId ?? request.CurrentDepartmentId;
            if (!departmentId.HasValue)
                throw new InvalidOperationException("Department is required when appointing a department manager.");

            var department = await _departmentRepo.GetByIdAsync(departmentId.Value, ct)
                ?? throw new KeyNotFoundException("Department was not found.");

            if (department.ManagerId == employeeId)
                return;

            await AddEmploymentHistoryAsync(
                employeeId,
                HistoryType.Appointment,
                $"DepartmentManagerId: {department.ManagerId?.ToString() ?? "null"}",
                $"DepartmentManagerId: {employeeId}",
                effectiveDate,
                ct);
            department.ManagerId = employeeId;
            _departmentRepo.Update(department);
        }

        private Task AddEmploymentHistoryAsync(
            int employeeId,
            HistoryType type,
            string oldValue,
            string newValue,
            DateTime effectiveDate,
            CancellationToken ct)
        {
            return _historyRepo.AddAsync(new EmploymentHistory
            {
                EmployeeId = employeeId,
                Type = type,
                OldValue = oldValue,
                NewValue = newValue,
                EffectiveDate = effectiveDate,
                ChangeDate = DateTime.UtcNow
            }, ct);
        }

        private async Task EnsureSelectedEmployeeCanConsentAsync(PersonnelChangeRequest request, int actorAccountId, CancellationToken ct)
        {
            if (!request.EmployeeId.HasValue)
                throw new InvalidOperationException("Senior appointment has no selected employee.");

            var employee = await _employeeRepo.GetByIdAsync(request.EmployeeId.Value, ct)
                ?? throw new KeyNotFoundException("Employee was not found.");

            if (employee.AccountId.HasValue && employee.AccountId.Value != actorAccountId)
                throw new UnauthorizedAccessException("Only the appointed employee can submit consent.");
        }

        private async Task AddHistoryAndSnapshotAsync(
            int requestId,
            string action,
            PersonnelChangeStatus? oldStatus,
            PersonnelChangeStatus? newStatus,
            int actorAccountId,
            string? note,
            CancellationToken ct)
        {
            await AddHistoryAsync(requestId, action, oldStatus, newStatus, actorAccountId, note, ct);
            await _unitOfWork.CommitAsync(ct);
            await SaveSnapshotAsync(requestId, actorAccountId, ct);
        }

        private async Task SaveSnapshotAsync(int requestId, int actorAccountId, CancellationToken ct)
        {
            var snapshotJson = await _riskSummaryBuilder.BuildSnapshotJsonAsync(requestId, ct);
            await _personnelChangeRepo.AddRiskSnapshotAsync(new PersonnelChangeRiskSnapshot
            {
                RequestId = requestId,
                SnapshotJson = snapshotJson,
                CreatedByAccountId = actorAccountId,
                CreatedAt = DateTime.UtcNow
            }, ct);
            await _unitOfWork.CommitAsync(ct);
        }

        private Task AddHistoryAsync(
            int requestId,
            string action,
            PersonnelChangeStatus? oldStatus,
            PersonnelChangeStatus? newStatus,
            int? actorAccountId,
            string? note,
            CancellationToken ct)
        {
            return _personnelChangeRepo.AddHistoryAsync(new PersonnelChangeHistory
            {
                RequestId = requestId,
                Action = action,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                ActorAccountId = actorAccountId,
                Note = note,
                CreatedAt = DateTime.UtcNow
            }, ct);
        }

        private static void EnsureSeniorAppointment(PersonnelChangeRequest request)
        {
            if (request.ChangeType != PersonnelChangeType.SeniorAppointment)
                throw new InvalidOperationException("Request is not a senior appointment request.");
        }

        private static void EnsureStatus(PersonnelChangeRequest request, params PersonnelChangeStatus[] allowedStatuses)
        {
            if (!PersonnelChangeStatusGuard.IsAllowed(request, allowedStatuses))
                throw new InvalidOperationException($"Request is in status {request.Status}, expected one of: {PersonnelChangeStatusGuard.DescribeAllowed(allowedStatuses)}.");
        }
    }
}

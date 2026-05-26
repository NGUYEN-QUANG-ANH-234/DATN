using System.Text.Json;
using HRM.backend.src.HRM.Application.DTOs.EmployeeProfile;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.EmployeeProfile.Usecases;
using HRM.backend.src.HRM.Application.Interfaces.System.Services;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.RequestHandover;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;

namespace HRM.backend.src.HRM.Application.UseCases.EmployeeProfile
{
    public class ContractAddendumUseCase : IContractAddendumUseCase
    {
        private readonly IContractRepository _contractRepo;
        private readonly IContractAddendumRepository _addendumRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IBaseRepository<EmploymentHistory> _historyRepo;
        private readonly IApprovalConflictGuard _approvalConflictGuard;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILockService _lockService;
        private readonly IIdempotencyService _idempotencyService;

        public ContractAddendumUseCase(
            IContractRepository contractRepo,
            IContractAddendumRepository addendumRepo,
            IEmployeeRepository employeeRepo,
            IBaseRepository<EmploymentHistory> historyRepo,
            IApprovalConflictGuard approvalConflictGuard,
            IUnitOfWork unitOfWork,
            ILockService lockService,
            IIdempotencyService idempotencyService)
        {
            _contractRepo = contractRepo;
            _addendumRepo = addendumRepo;
            _employeeRepo = employeeRepo;
            _historyRepo = historyRepo;
            _approvalConflictGuard = approvalConflictGuard;
            _unitOfWork = unitOfWork;
            _lockService = lockService;
            _idempotencyService = idempotencyService;
        }

        public async Task<ContractAddendumResponseDto> CreateDraftAsync(int contractId, CreateContractAddendumDto dto, CancellationToken ct, string? idempotencyKey = null)
        {
            var existingResourceId = string.IsNullOrWhiteSpace(idempotencyKey)
                ? null
                : await _idempotencyService.FindResourceIdAsync("CONTRACT_ADDENDUM_CREATE", idempotencyKey, ct);
            if (existingResourceId.HasValue)
            {
                var existing = await _addendumRepo.GetByIdWithContractAsync(existingResourceId.Value, ct);
                if (existing != null)
                    return Map(existing);
            }

            ValidateDraft(dto);

            var addendumId = await _lockService.GetWithLockAsync($"addendum_create_{contractId}", async (innerCt) =>
            {
                ContractAddendum? addendum = null;
                await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                var contract = await _contractRepo.GetByIdAsync(contractId, innerCt);
                if (contract == null)
                    throw new InvalidOperationException("Không tìm thấy hợp đồng gốc.");
                if (contract.Status != ContractStatus.Active)
                    throw new InvalidOperationException("Chỉ có thể tạo phụ lục cho hợp đồng đang có hiệu lực.");

                addendum = new ContractAddendum
                {
                    ContractId = contractId,
                    AddendumNumber = GenerateAddendumNumber(contractId),
                    Status = AddendumStatus.Draft
                };
                ApplyDraftFields(addendum, dto);

                await _addendumRepo.AddAsync(addendum, innerCt);
                await _unitOfWork.CommitAsync(innerCt);
                await _idempotencyService.SaveAsync("CONTRACT_ADDENDUM_CREATE", idempotencyKey ?? string.Empty, "ContractAddendum", addendum.Id, null, innerCt);
                await _unitOfWork.CommitAsync(innerCt);
                }, innerCt);
                return addendum!.Id;
            }, cancellationToken: ct);

            var created = await _addendumRepo.GetByIdWithContractAsync(addendumId, ct);
            return Map(created!);
        }

        public async Task<ContractAddendumResponseDto> UpdateDraftAsync(int addendumId, CreateContractAddendumDto dto, CancellationToken ct)
        {
            ValidateDraft(dto);

            await _lockService.GetWithLockAsync($"addendum_{addendumId}", async (innerCt) =>
            {
                await _unitOfWork.ExecuteTransactionAsync(async () =>
                {
                var addendum = await _addendumRepo.GetByIdWithContractAsync(addendumId, innerCt);
                if (addendum == null)
                    throw new InvalidOperationException("Không tìm thấy phụ lục hợp đồng.");
                if (addendum.Status != AddendumStatus.Draft)
                    throw new InvalidOperationException("Chỉ có thể sửa bản nháp phụ lục.");
                if (addendum.Contract == null || addendum.Contract.Status != ContractStatus.Active)
                    throw new InvalidOperationException("Hợp đồng gốc không còn ở trạng thái có hiệu lực.");

                ApplyDraftFields(addendum, dto);
                await _addendumRepo.UpdateAsync(addendum, innerCt);
                await _unitOfWork.CommitAsync(innerCt);
                }, innerCt);
                return true;
            }, cancellationToken: ct);

            var updated = await _addendumRepo.GetByIdWithContractAsync(addendumId, ct);
            return Map(updated!);
        }

        public async Task SubmitAsync(int addendumId, CancellationToken ct)
        {
            await _lockService.GetWithLockAsync($"addendum_{addendumId}", async (innerCt) =>
            {
            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                var addendum = await _addendumRepo.GetByIdWithContractAsync(addendumId, innerCt);
                if (addendum == null)
                    throw new InvalidOperationException("Không tìm thấy phụ lục hợp đồng.");
                if (addendum.Status != AddendumStatus.Draft)
                    throw new InvalidOperationException("Chỉ bản nháp phụ lục mới có thể gửi duyệt.");

                if (addendum.Contract?.EmployeeId.HasValue != true)
                    throw new InvalidOperationException("Phu luc chua lien ket nhan vien.");

                var targetEmployeeId = addendum.Contract.EmployeeId.GetValueOrDefault();
                var targetRoleName = await _approvalConflictGuard.GetEmployeeRoleNameAsync(targetEmployeeId, innerCt);
                addendum.Status = IsHr(targetRoleName) || IsManager(targetRoleName)
                    ? AddendumStatus.PendingHR
                    : AddendumStatus.PendingDept;
                await _addendumRepo.UpdateAsync(addendum, innerCt);
                await _unitOfWork.CommitAsync(innerCt);
            }, innerCt);
            return true;
            }, cancellationToken: ct);
        }

        public async Task ReviewByDeptAsync(int addendumId, int actorAccountId, string actorRoleName, ReviewContractAddendumDto dto, CancellationToken ct)
        {
            await _lockService.GetWithLockAsync($"addendum_{addendumId}", async (innerCt) =>
            {
            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                var addendum = await _addendumRepo.GetByIdWithContractAsync(addendumId, innerCt);
                if (addendum == null)
                    throw new InvalidOperationException("KhÃ´ng tÃ¬m tháº¥y phá»¥ lá»¥c há»£p Ä‘á»“ng.");
                if (addendum.Status != AddendumStatus.PendingDept)
                    throw new InvalidOperationException("Phá»¥ lá»¥c khÃ´ng á»Ÿ tráº¡ng thÃ¡i chá» TrÆ°á»Ÿng phÃ²ng xÃ¡c nháº­n.");

                await EnsureManagerCanAccessAsync(addendum, actorAccountId, actorRoleName, innerCt);
                await _approvalConflictGuard.EnsureNotSelfApprovalForEmployeeAsync(GetTargetEmployeeId(addendum), actorAccountId, innerCt);

                addendum.Status = dto.IsApproved ? AddendumStatus.PendingHR : AddendumStatus.Rejected;
                addendum.RejectReason = dto.IsApproved
                    ? null
                    : string.IsNullOrWhiteSpace(dto.RejectReason)
                        ? "TrÆ°á»Ÿng phÃ²ng tá»« chá»‘i phá»¥ lá»¥c há»£p Ä‘á»“ng."
                        : dto.RejectReason.Trim();

                await _addendumRepo.UpdateAsync(addendum, innerCt);
                await _unitOfWork.CommitAsync(innerCt);
            }, innerCt);
            return true;
            }, cancellationToken: ct);
        }

        public async Task ConfirmByHrAsync(int addendumId, int actorAccountId, string actorRoleName, ReviewContractAddendumDto dto, CancellationToken ct)
        {
            await _lockService.GetWithLockAsync($"addendum_{addendumId}", async (innerCt) =>
            {
            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                var addendum = await _addendumRepo.GetByIdWithContractAsync(addendumId, innerCt);
                if (addendum == null)
                    throw new InvalidOperationException("KhÃ´ng tÃ¬m tháº¥y phá»¥ lá»¥c há»£p Ä‘á»“ng.");
                if (addendum.Status != AddendumStatus.PendingHR)
                    throw new InvalidOperationException("Phá»¥ lá»¥c khÃ´ng á»Ÿ tráº¡ng thÃ¡i chá» HR xÃ¡c nháº­n chÃ­nh sÃ¡ch.");

                EnsureHrDirectorOrAdmin(actorRoleName);
                if (!IsDirector(actorRoleName) && !IsAdmin(actorRoleName))
                    await _approvalConflictGuard.EnsureNotSelfApprovalForEmployeeAsync(GetTargetEmployeeId(addendum), actorAccountId, innerCt);

                addendum.Status = dto.IsApproved ? AddendumStatus.PendingEmployee : AddendumStatus.Rejected;
                addendum.RejectReason = dto.IsApproved
                    ? null
                    : string.IsNullOrWhiteSpace(dto.RejectReason)
                        ? "HR tá»« chá»‘i phá»¥ lá»¥c do khÃ´ng Ä‘Ã¡p á»©ng chÃ­nh sÃ¡ch."
                        : dto.RejectReason.Trim();

                await _addendumRepo.UpdateAsync(addendum, innerCt);
                await _unitOfWork.CommitAsync(innerCt);
            }, innerCt);
            return true;
            }, cancellationToken: ct);
        }

        public async Task EmployeeConfirmAsync(int addendumId, int actorAccountId, ReviewContractAddendumDto dto, CancellationToken ct)
        {
            await _lockService.GetWithLockAsync($"addendum_{addendumId}", async (innerCt) =>
            {
            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                var addendum = await _addendumRepo.GetByIdWithContractAsync(addendumId, innerCt);
                if (addendum == null)
                    throw new InvalidOperationException("Khong tim thay phu luc hop dong.");
                if (addendum.Status != AddendumStatus.PendingEmployee)
                    throw new InvalidOperationException("Phu luc khong o trang thai cho nguoi lao dong xac nhan.");

                await EnsureEmployeeOwnsAddendumAsync(addendum, actorAccountId, innerCt);

                addendum.Status = dto.IsApproved ? AddendumStatus.PendingDirector : AddendumStatus.Rejected;
                addendum.RejectReason = dto.IsApproved
                    ? null
                    : string.IsNullOrWhiteSpace(dto.RejectReason)
                        ? "Nguoi lao dong tu choi dieu khoan phu luc hop dong."
                        : dto.RejectReason.Trim();

                await _addendumRepo.UpdateAsync(addendum, innerCt);
                await _unitOfWork.CommitAsync(innerCt);
            }, innerCt);
            return true;
            }, cancellationToken: ct);
        }

        public async Task ApproveAsync(int addendumId, int actorAccountId, string actorRoleName, CancellationToken ct)
        {
            await _lockService.GetWithLockAsync($"addendum_{addendumId}", async (innerCt) =>
            {
            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                var addendum = await _addendumRepo.GetByIdWithContractAsync(addendumId, innerCt);
                if (addendum == null)
                    throw new InvalidOperationException("Không tìm thấy phụ lục hợp đồng.");
                if (addendum.Status != AddendumStatus.PendingDirector)
                    throw new InvalidOperationException("Phụ lục không ở trạng thái chờ Giám đốc phê duyệt.");

                var contract = addendum.Contract ?? throw new InvalidOperationException("Phụ lục chưa liên kết hợp đồng gốc.");
                if (!contract.EmployeeId.HasValue)
                    throw new InvalidOperationException("Hợp đồng gốc chưa gắn nhân viên.");

                EnsureDirectorOrAdmin(actorRoleName);
                await _approvalConflictGuard.EnsureNotSelfApprovalForEmployeeAsync(contract.EmployeeId.Value, actorAccountId, innerCt);

                var employee = await _employeeRepo.GetByIdAsync(contract.EmployeeId.Value, innerCt)
                    ?? throw new InvalidOperationException("Không tìm thấy nhân viên của hợp đồng.");

                await ApplySalaryChangesAsync(addendum, contract, employee.Id, innerCt);
                await ApplyContractTermChangeAsync(addendum, contract, employee.Id, innerCt);
                await ApplyOtherChangesAsync(addendum, employee, innerCt);

                addendum.Status = AddendumStatus.Active;
                addendum.RejectReason = null;

                await _addendumRepo.UpdateAsync(addendum, innerCt);
                await _contractRepo.UpdateAsync(contract, innerCt);
                await _unitOfWork.CommitAsync(innerCt);
            }, innerCt);
            return true;
            }, cancellationToken: ct);
        }

        public async Task RejectAsync(int addendumId, int actorAccountId, string actorRoleName, string? reason, CancellationToken ct)
        {
            await _lockService.GetWithLockAsync($"addendum_{addendumId}", async (innerCt) =>
            {
            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                var addendum = await _addendumRepo.GetByIdWithContractAsync(addendumId, innerCt);
                if (addendum == null)
                    throw new InvalidOperationException("Không tìm thấy phụ lục hợp đồng.");
                if (addendum.Status != AddendumStatus.PendingDirector)
                    throw new InvalidOperationException("Chỉ phụ lục đang chờ duyệt mới có thể bị từ chối.");

                EnsureDirectorOrAdmin(actorRoleName);
                await _approvalConflictGuard.EnsureNotSelfApprovalForEmployeeAsync(GetTargetEmployeeId(addendum), actorAccountId, innerCt);

                addendum.Status = AddendumStatus.Rejected;
                addendum.RejectReason = string.IsNullOrWhiteSpace(reason)
                    ? "Giám đốc từ chối phụ lục hợp đồng."
                    : reason.Trim();

                await _addendumRepo.UpdateAsync(addendum, innerCt);
                await _unitOfWork.CommitAsync(innerCt);
            }, innerCt);
            return true;
            }, cancellationToken: ct);
        }

        public async Task<IEnumerable<ContractAddendumResponseDto>> GetByContractAsync(int contractId, CancellationToken ct)
        {
            var addendums = await _addendumRepo.GetByContractIdAsync(contractId, ct);
            return addendums.Select(Map);
        }

        public async Task<IEnumerable<ContractAddendumResponseDto>> GetMyPendingEmployeeAsync(int actorAccountId, CancellationToken ct)
        {
            var employee = await _employeeRepo.GetByAccountIdAsync(actorAccountId, ct);
            if (employee == null) return Enumerable.Empty<ContractAddendumResponseDto>();

            var addendums = await _addendumRepo.GetByStatusAsync(AddendumStatus.PendingEmployee, ct);
            return addendums
                .Where(a => a.Contract?.EmployeeId == employee.Id)
                .Select(Map);
        }

        public async Task<IEnumerable<ContractAddendumResponseDto>> GetPendingDeptAsync(int actorAccountId, string actorRoleName, CancellationToken ct)
        {
            var addendums = await _addendumRepo.GetByStatusAsync(AddendumStatus.PendingDept, ct);

            if (IsAdmin(actorRoleName))
                return addendums.Select(Map);

            if (!IsManager(actorRoleName))
                throw new UnauthorizedAccessException("Chá»‰ TrÆ°á»Ÿng phÃ²ng hoáº·c Admin Ä‘Æ°á»£c xem phá»¥ lá»¥c chá» xÃ¡c nháº­n nghiá»‡p vá»¥.");

            var manager = await _employeeRepo.GetByAccountIdAsync(actorAccountId, ct)
                ?? throw new UnauthorizedAccessException("TÃ i khoáº£n TrÆ°á»Ÿng phÃ²ng chÆ°a liÃªn káº¿t há»“ sÆ¡ nhÃ¢n sá»±.");

            return addendums
                .Where(a => a.Contract?.Employee?.DeptId.HasValue == true &&
                            manager.DeptId.HasValue &&
                            a.Contract.Employee.DeptId.Value == manager.DeptId.Value)
                .Select(Map);
        }

        public async Task<IEnumerable<ContractAddendumResponseDto>> GetPendingHRAsync(CancellationToken ct)
        {
            var addendums = await _addendumRepo.GetByStatusAsync(AddendumStatus.PendingHR, ct);
            return addendums.Select(Map);
        }

        public async Task<IEnumerable<ContractAddendumResponseDto>> GetPendingDirectorAsync(CancellationToken ct)
        {
            var addendums = await _addendumRepo.GetByStatusAsync(AddendumStatus.PendingDirector, ct);
            return addendums.Select(Map);
        }

        public async Task<IEnumerable<ContractAddendumResponseDto>> GetAllAsync(CancellationToken ct)
        {
            var addendums = await _addendumRepo.GetAllWithContractAsync(ct);
            return addendums.Select(Map);
        }

        private static void ValidateDraft(CreateContractAddendumDto dto)
        {
            if (dto.EffectiveDate == default)
                throw new ArgumentException("Ngày hiệu lực phụ lục không hợp lệ.");
            if (dto.NewBasicSalary is <= 0)
                throw new ArgumentException("Lương cơ bản mới phải lớn hơn 0.");
            if (dto.NewInsuranceSalary is < 0)
                throw new ArgumentException("Lương đóng bảo hiểm mới không được âm.");
            if (dto.NewBasicSalary == null &&
                dto.NewInsuranceSalary == null &&
                dto.NewEndDate == null &&
                string.IsNullOrWhiteSpace(dto.OtherChangesJson))
                throw new ArgumentException("Phụ lục cần có ít nhất một nội dung điều chỉnh.");
        }

        private static string? NormalizeJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            using var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc.RootElement);
        }

        private static void ApplyDraftFields(ContractAddendum addendum, CreateContractAddendumDto dto)
        {
            addendum.NewBasicSalary = dto.NewBasicSalary;
            addendum.NewInsuranceSalary = dto.NewInsuranceSalary;
            addendum.NewEndDate = dto.NewEndDate;
            addendum.OtherChangesJson = NormalizeJson(dto.OtherChangesJson);
            addendum.Content = string.IsNullOrWhiteSpace(dto.Content) ? null : dto.Content.Trim();
            addendum.EffectiveDate = dto.EffectiveDate;
        }

        private static string GenerateAddendumNumber(int contractId)
        {
            return $"PL-{DateTime.UtcNow:yyyyMMddHHmmss}-{contractId:D4}";
        }

        private async Task ApplySalaryChangesAsync(ContractAddendum addendum, Contract contract, int employeeId, CancellationToken ct)
        {
            if (addendum.NewBasicSalary.HasValue && addendum.NewBasicSalary.Value != contract.BasicSalary)
            {
                await AddHistoryAsync(
                    employeeId,
                    HistoryType.Salary_Change,
                    $"BasicSalary: {contract.BasicSalary:N0}",
                    $"BasicSalary: {addendum.NewBasicSalary.Value:N0} (Addendum {addendum.AddendumNumber})",
                    addendum.EffectiveDate,
                    ct);
                contract.BasicSalary = addendum.NewBasicSalary.Value;
            }

            if (addendum.NewInsuranceSalary.HasValue && addendum.NewInsuranceSalary.Value != contract.InsuranceSalary)
            {
                await AddHistoryAsync(
                    employeeId,
                    HistoryType.Salary_Change,
                    $"InsuranceSalary: {contract.InsuranceSalary:N0}",
                    $"InsuranceSalary: {addendum.NewInsuranceSalary.Value:N0} (Addendum {addendum.AddendumNumber})",
                    addendum.EffectiveDate,
                    ct);
                contract.InsuranceSalary = addendum.NewInsuranceSalary.Value;
            }
        }

        private async Task ApplyContractTermChangeAsync(ContractAddendum addendum, Contract contract, int employeeId, CancellationToken ct)
        {
            if (!addendum.NewEndDate.HasValue || addendum.NewEndDate == contract.EndDate) return;

            await AddHistoryAsync(
                employeeId,
                HistoryType.Appointment,
                $"ContractEndDate: {FormatDate(contract.EndDate)}",
                $"ContractEndDate: {FormatDate(addendum.NewEndDate)} (Addendum {addendum.AddendumNumber})",
                addendum.EffectiveDate,
                ct);
            contract.EndDate = addendum.NewEndDate;
        }

        private async Task ApplyOtherChangesAsync(ContractAddendum addendum, Employee employee, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(addendum.OtherChangesJson)) return;

            using var doc = JsonDocument.Parse(addendum.OtherChangesJson);
            var root = doc.RootElement;

            if (TryGetInt(root, "deptId", out var newDeptId) && newDeptId != employee.DeptId)
            {
                await AddHistoryAsync(
                    employee.Id,
                    HistoryType.Transfer,
                    $"DeptId: {employee.DeptId?.ToString() ?? "null"}",
                    $"DeptId: {newDeptId} (Addendum {addendum.AddendumNumber})",
                    addendum.EffectiveDate,
                    ct);
                employee.DeptId = newDeptId;
            }

            if (TryGetInt(root, "positionId", out var newPositionId) && newPositionId != employee.PositionId)
            {
                await AddHistoryAsync(
                    employee.Id,
                    HistoryType.Appointment,
                    $"PositionId: {employee.PositionId?.ToString() ?? "null"}",
                    $"PositionId: {newPositionId} (Addendum {addendum.AddendumNumber})",
                    addendum.EffectiveDate,
                    ct);
                employee.PositionId = newPositionId;
            }
        }

        private async Task AddHistoryAsync(
            int employeeId,
            HistoryType type,
            string oldValue,
            string newValue,
            DateTime effectiveDate,
            CancellationToken ct)
        {
            await _historyRepo.AddAsync(new EmploymentHistory
            {
                EmployeeId = employeeId,
                Type = type,
                OldValue = oldValue,
                NewValue = newValue,
                EffectiveDate = effectiveDate,
                ChangeDate = DateTime.UtcNow
            }, ct);
        }

        private static bool TryGetInt(JsonElement root, string propertyName, out int value)
        {
            value = 0;
            if (!root.TryGetProperty(propertyName, out var property)) return false;
            if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out value)) return true;
            return property.ValueKind == JsonValueKind.String && int.TryParse(property.GetString(), out value);
        }

        private static string FormatDate(DateTime? value) =>
            value.HasValue ? value.Value.ToString("yyyy-MM-dd") : "null";

        private async Task EnsureManagerCanAccessAsync(ContractAddendum addendum, int actorAccountId, string actorRoleName, CancellationToken ct)
        {
            if (IsAdmin(actorRoleName))
                return;

            if (!IsManager(actorRoleName))
                throw new UnauthorizedAccessException("Chá»‰ TrÆ°á»Ÿng phÃ²ng Ä‘Æ°á»£c xÃ¡c nháº­n nghiá»‡p vá»¥ phá»¥ lá»¥c.");

            var manager = await _employeeRepo.GetByAccountIdAsync(actorAccountId, ct)
                ?? throw new UnauthorizedAccessException("TÃ i khoáº£n TrÆ°á»Ÿng phÃ²ng chÆ°a liÃªn káº¿t há»“ sÆ¡ nhÃ¢n sá»±.");

            var employeeDeptId = addendum.Contract?.Employee?.DeptId;
            if (!manager.DeptId.HasValue || !employeeDeptId.HasValue || manager.DeptId.Value != employeeDeptId.Value)
                throw new UnauthorizedAccessException("TrÆ°á»Ÿng phÃ²ng chá»‰ Ä‘Æ°á»£c xÃ¡c nháº­n phá»¥ lá»¥c cá»§a nhÃ¢n viÃªn trong phÃ²ng ban mÃ¬nh.");
        }

        private static int GetTargetEmployeeId(ContractAddendum addendum)
        {
            return addendum.Contract?.EmployeeId
                ?? throw new InvalidOperationException("Phu luc chua gan voi nhan vien.");
        }

        private async Task EnsureEmployeeOwnsAddendumAsync(ContractAddendum addendum, int actorAccountId, CancellationToken ct)
        {
            var employeeId = GetTargetEmployeeId(addendum);
            var employee = await _employeeRepo.GetProfileByIdAsync(employeeId, ct)
                ?? throw new InvalidOperationException("Khong tim thay nhan vien cua phu luc.");

            if (!employee.AccountId.HasValue || employee.AccountId.Value != actorAccountId)
                throw new UnauthorizedAccessException("Chi nguoi lao dong cua phu luc moi duoc xac nhan dieu khoan.");
        }

        private static void EnsureHrDirectorOrAdmin(string actorRoleName)
        {
            if (!IsHr(actorRoleName) && !IsDirector(actorRoleName) && !IsAdmin(actorRoleName))
                throw new UnauthorizedAccessException("Chi HR, Giam doc hoac Admin duoc xac nhan chinh sach phu luc.");
        }

        private static void EnsureDirectorOrAdmin(string actorRoleName)
        {
            if (!IsDirector(actorRoleName) && !IsAdmin(actorRoleName))
                throw new UnauthorizedAccessException("Chi Giam doc hoac Admin duoc phe duyet cuoi phu luc.");
        }

        private static bool IsAdmin(string role) => string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
        private static bool IsManager(string? role) => string.Equals(role, "Manager", StringComparison.OrdinalIgnoreCase);
        private static bool IsHr(string? role) => string.Equals(role, "HR", StringComparison.OrdinalIgnoreCase);
        private static bool IsDirector(string? role) => string.Equals(role, "Director", StringComparison.OrdinalIgnoreCase);

        private static ContractAddendumResponseDto Map(ContractAddendum addendum) => new()
        {
            Id = addendum.Id,
            ContractId = addendum.ContractId,
            ContractNumber = addendum.Contract?.ContractNumber ?? string.Empty,
            AddendumNumber = addendum.AddendumNumber,
            NewBasicSalary = addendum.NewBasicSalary,
            NewInsuranceSalary = addendum.NewInsuranceSalary,
            NewEndDate = addendum.NewEndDate,
            OtherChangesJson = addendum.OtherChangesJson,
            Content = addendum.Content,
            EffectiveDate = addendum.EffectiveDate,
            Status = addendum.Status.ToString(),
            RejectReason = addendum.RejectReason,
            CreatedAt = addendum.CreatedAt,
            EmployeeId = addendum.Contract?.EmployeeId,
            EmployeeName = addendum.Contract?.Employee?.FullName
        };
    }
}

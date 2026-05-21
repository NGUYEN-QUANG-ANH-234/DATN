using System.Text.Json;
using HRM.backend.src.HRM.Application.DTOs.EmployeeProfile;
using HRM.backend.src.HRM.Application.Interfaces.EmployeeProfile.Usecases;
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
        private readonly IUnitOfWork _unitOfWork;

        public ContractAddendumUseCase(
            IContractRepository contractRepo,
            IContractAddendumRepository addendumRepo,
            IEmployeeRepository employeeRepo,
            IBaseRepository<EmploymentHistory> historyRepo,
            IUnitOfWork unitOfWork)
        {
            _contractRepo = contractRepo;
            _addendumRepo = addendumRepo;
            _employeeRepo = employeeRepo;
            _historyRepo = historyRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task<ContractAddendumResponseDto> CreateDraftAsync(int contractId, CreateContractAddendumDto dto, CancellationToken ct)
        {
            ValidateDraft(dto);

            ContractAddendum? addendum = null;
            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                var contract = await _contractRepo.GetByIdAsync(contractId, ct);
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

                await _addendumRepo.AddAsync(addendum, ct);
                await _unitOfWork.CommitAsync(ct);
            }, ct);

            var created = await _addendumRepo.GetByIdWithContractAsync(addendum!.Id, ct);
            return Map(created!);
        }

        public async Task<ContractAddendumResponseDto> UpdateDraftAsync(int addendumId, CreateContractAddendumDto dto, CancellationToken ct)
        {
            ValidateDraft(dto);

            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                var addendum = await _addendumRepo.GetByIdWithContractAsync(addendumId, ct);
                if (addendum == null)
                    throw new InvalidOperationException("Không tìm thấy phụ lục hợp đồng.");
                if (addendum.Status != AddendumStatus.Draft)
                    throw new InvalidOperationException("Chỉ có thể sửa bản nháp phụ lục.");
                if (addendum.Contract == null || addendum.Contract.Status != ContractStatus.Active)
                    throw new InvalidOperationException("Hợp đồng gốc không còn ở trạng thái có hiệu lực.");

                ApplyDraftFields(addendum, dto);
                await _addendumRepo.UpdateAsync(addendum, ct);
                await _unitOfWork.CommitAsync(ct);
            }, ct);

            var updated = await _addendumRepo.GetByIdWithContractAsync(addendumId, ct);
            return Map(updated!);
        }

        public async Task SubmitAsync(int addendumId, CancellationToken ct)
        {
            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                var addendum = await _addendumRepo.GetByIdWithContractAsync(addendumId, ct);
                if (addendum == null)
                    throw new InvalidOperationException("Không tìm thấy phụ lục hợp đồng.");
                if (addendum.Status != AddendumStatus.Draft)
                    throw new InvalidOperationException("Chỉ bản nháp phụ lục mới có thể gửi duyệt.");

                addendum.Status = AddendumStatus.PendingDirector;
                await _addendumRepo.UpdateAsync(addendum, ct);
                await _unitOfWork.CommitAsync(ct);
            }, ct);
        }

        public async Task ApproveAsync(int addendumId, CancellationToken ct)
        {
            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                var addendum = await _addendumRepo.GetByIdWithContractAsync(addendumId, ct);
                if (addendum == null)
                    throw new InvalidOperationException("Không tìm thấy phụ lục hợp đồng.");
                if (addendum.Status != AddendumStatus.PendingDirector)
                    throw new InvalidOperationException("Phụ lục không ở trạng thái chờ Giám đốc phê duyệt.");

                var contract = addendum.Contract ?? throw new InvalidOperationException("Phụ lục chưa liên kết hợp đồng gốc.");
                if (!contract.EmployeeId.HasValue)
                    throw new InvalidOperationException("Hợp đồng gốc chưa gắn nhân viên.");

                var employee = await _employeeRepo.GetByIdAsync(contract.EmployeeId.Value, ct)
                    ?? throw new InvalidOperationException("Không tìm thấy nhân viên của hợp đồng.");

                await ApplySalaryChangesAsync(addendum, contract, employee.Id, ct);
                await ApplyContractTermChangeAsync(addendum, contract, employee.Id, ct);
                await ApplyOtherChangesAsync(addendum, employee, ct);

                addendum.Status = AddendumStatus.Active;
                addendum.RejectReason = null;

                await _addendumRepo.UpdateAsync(addendum, ct);
                await _contractRepo.UpdateAsync(contract, ct);
                await _employeeRepo.UpdateAsync(employee, ct);
                await _unitOfWork.CommitAsync(ct);
            }, ct);
        }

        public async Task RejectAsync(int addendumId, string? reason, CancellationToken ct)
        {
            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                var addendum = await _addendumRepo.GetByIdWithContractAsync(addendumId, ct);
                if (addendum == null)
                    throw new InvalidOperationException("Không tìm thấy phụ lục hợp đồng.");
                if (addendum.Status != AddendumStatus.PendingDirector)
                    throw new InvalidOperationException("Chỉ phụ lục đang chờ duyệt mới có thể bị từ chối.");

                addendum.Status = AddendumStatus.Rejected;
                addendum.RejectReason = string.IsNullOrWhiteSpace(reason)
                    ? "Giám đốc từ chối phụ lục hợp đồng."
                    : reason.Trim();

                await _addendumRepo.UpdateAsync(addendum, ct);
                await _unitOfWork.CommitAsync(ct);
            }, ct);
        }

        public async Task<IEnumerable<ContractAddendumResponseDto>> GetByContractAsync(int contractId, CancellationToken ct)
        {
            var addendums = await _addendumRepo.GetByContractIdAsync(contractId, ct);
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

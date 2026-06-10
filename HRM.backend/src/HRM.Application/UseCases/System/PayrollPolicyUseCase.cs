using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;

namespace HRM.backend.src.HRM.Application.UseCases.System
{
    public class PayrollPolicyUseCase : IPayrollPolicyUseCase
    {
        private const string CachePrefix = "PayrollPolicies";

        private readonly IPayrollPolicyRepository _policyRepo;
        private readonly IAuditLogRepository _auditLogRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAppCache _cache;
        private readonly ILockService _lockService;

        public PayrollPolicyUseCase(
            IPayrollPolicyRepository policyRepo,
            IAuditLogRepository auditLogRepo,
            IUnitOfWork unitOfWork,
            IAppCache cache,
            ILockService lockService)
        {
            _policyRepo = policyRepo;
            _auditLogRepo = auditLogRepo;
            _unitOfWork = unitOfWork;
            _cache = cache;
            _lockService = lockService;
        }

        public async Task<List<PayrollPolicyDto>> GetPoliciesAsync(PayrollPolicyFilterDto filter, CancellationToken ct = default)
        {
            var cacheKey = $"{CachePrefix}_{filter.PolicyType?.ToString() ?? "All"}_{filter.IncludeInactive}";
            return await _cache.GetOrSetWithLockAsync(
                cacheKey,
                async (innerCt) =>
                {
                    var policies = await _policyRepo.GetByFilterAsync(filter.PolicyType, filter.IncludeInactive, innerCt);
                    return policies.Select(MapToDto).ToList();
                },
                TimeSpan.FromHours(24),
                _lockService,
                ct: ct);
        }

        public async Task<PayrollPolicyDto> CreatePolicyAsync(CreatePayrollPolicyDto dto, int actorId, CancellationToken ct = default)
        {
            Normalize(dto);
            Validate(dto);

            return await _lockService.GetWithLockAsync(
                BuildLockKey(dto.PolicyType, dto.Code),
                async (innerCt) =>
                {
                    await EnsureNoOverlapAsync(dto.PolicyType, dto.Code, dto.EffectiveFrom, dto.EffectiveTo, null, innerCt);

                    var entity = new PayrollPolicy
                    {
                        PolicyType = dto.PolicyType,
                        Code = dto.Code,
                        Name = dto.Name.Trim(),
                        ValueType = dto.ValueType,
                        RatePercent = dto.RatePercent,
                        Amount = dto.Amount,
                        FromAmount = dto.FromAmount,
                        ToAmount = dto.ToAmount,
                        QuickDeduction = dto.QuickDeduction,
                        FormulaJson = dto.FormulaJson,
                        EffectiveFrom = dto.EffectiveFrom.Date,
                        EffectiveTo = dto.EffectiveTo?.Date,
                        Version = dto.Version <= 0 ? 1 : dto.Version,
                        VersionCode = BuildVersionCode(dto.Code, dto.Version <= 0 ? 1 : dto.Version, dto.VersionCode),
                        Status = dto.Status,
                        SourceRef = dto.SourceRef,
                        ActivatedAt = dto.Status == PolicyVersionStatus.Active ? DateTime.UtcNow : null,
                        IsActive = dto.IsActive,
                        Description = dto.Description,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByAccountId = actorId
                    };

                    await _unitOfWork.ExecuteTransactionAsync(async () =>
                    {
                        await _policyRepo.AddAsync(entity, innerCt);
                        await _auditLogRepo.LogSystemEventAsync("PAYROLL_POLICY_CREATE", actorId, "payroll_policies", entity.Code);
                        await _unitOfWork.CommitAsync(innerCt);
                    }, innerCt);

                    await RemovePolicyCachesAsync(innerCt);
                    return MapToDto(entity);
                },
                cancellationToken: ct);
        }

        public async Task<PayrollPolicyDto> UpdatePolicyAsync(int id, UpdatePayrollPolicyDto dto, int actorId, CancellationToken ct = default)
        {
            Normalize(dto);
            Validate(dto);

            return await _lockService.GetWithLockAsync(
                BuildLockKey(dto.PolicyType, dto.Code),
                async (innerCt) =>
                {
                    var entity = await _policyRepo.GetByIdForUpdateAsync(id, innerCt)
                        ?? throw new InvalidOperationException("Không tìm thấy chính sách lương.");

                    if (entity.PolicyType != dto.PolicyType || !string.Equals(entity.Code, dto.Code, StringComparison.OrdinalIgnoreCase))
                        throw new ArgumentException("Không được đổi loại hoặc mã chính sách khi tạo phiên bản mới. Hãy tạo chính sách mới.");

                    var newEffectiveFrom = dto.EffectiveFrom.Date;
                    if (newEffectiveFrom <= entity.EffectiveFrom.Date)
                        throw new ArgumentException("Ngay hieu luc cua phiên bản moi phai sau ngay hieu luc cua phiên bản hien tai.");

                    await EnsureNoOverlapAsync(dto.PolicyType, dto.Code, newEffectiveFrom, dto.EffectiveTo, id, innerCt);

                    var samePolicies = await _policyRepo.GetByTypeAndCodeAsync(dto.PolicyType, dto.Code, innerCt);
                    var nextVersion = samePolicies.Any()
                        ? samePolicies.Max(x => x.Version) + 1
                        : entity.Version + 1;

                    var closeDate = newEffectiveFrom.AddDays(-1);
                    if (!entity.EffectiveTo.HasValue || entity.EffectiveTo.Value.Date > closeDate)
                        entity.EffectiveTo = closeDate;

                    entity.Status = PolicyVersionStatus.Archived;
                    entity.UpdatedAt = DateTime.UtcNow;
                    entity.UpdatedByAccountId = actorId;

                    var newEntity = new PayrollPolicy
                    {
                        PolicyType = dto.PolicyType,
                        Code = dto.Code,
                        Name = dto.Name.Trim(),
                        ValueType = dto.ValueType,
                        RatePercent = dto.RatePercent,
                        Amount = dto.Amount,
                        FromAmount = dto.FromAmount,
                        ToAmount = dto.ToAmount,
                        QuickDeduction = dto.QuickDeduction,
                        FormulaJson = dto.FormulaJson,
                        EffectiveFrom = newEffectiveFrom,
                        EffectiveTo = dto.EffectiveTo?.Date,
                        Version = Math.Max(dto.Version, nextVersion),
                        VersionCode = BuildVersionCode(dto.Code, Math.Max(dto.Version, nextVersion), dto.VersionCode),
                        Status = dto.Status,
                        SourceRef = dto.SourceRef,
                        SupersedesVersionId = entity.Id,
                        ActivatedAt = dto.Status == PolicyVersionStatus.Active ? DateTime.UtcNow : null,
                        IsActive = dto.IsActive,
                        Description = dto.Description,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByAccountId = actorId
                    };

                    await _unitOfWork.ExecuteTransactionAsync(async () =>
                    {
                        _policyRepo.Update(entity);
                        await _policyRepo.AddAsync(newEntity, innerCt);
                        await _auditLogRepo.LogSystemEventAsync("PAYROLL_POLICY_VERSION_CREATE", actorId, "payroll_policies", $"{entity.Code}:v{entity.Version}->v{newEntity.Version}");
                        await _unitOfWork.CommitAsync(innerCt);
                    }, innerCt);

                    await RemovePolicyCachesAsync(innerCt);
                    return MapToDto(newEntity);
                },
                cancellationToken: ct);
        }

        public async Task<bool> SetActiveAsync(int id, bool isActive, int actorId, CancellationToken ct = default)
        {
            var entity = await _policyRepo.GetByIdForUpdateAsync(id, ct)
                ?? throw new InvalidOperationException("Không tìm thấy chính sách lương.");

            entity.IsActive = isActive;
            entity.Status = isActive ? PolicyVersionStatus.Active : PolicyVersionStatus.Archived;
            if (isActive && !entity.ActivatedAt.HasValue)
                entity.ActivatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedByAccountId = actorId;

            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                _policyRepo.Update(entity);
                await _auditLogRepo.LogSystemEventAsync("PAYROLL_POLICY_STATUS", actorId, "payroll_policies", $"{entity.Code}:{isActive}");
                await _unitOfWork.CommitAsync(ct);
            }, ct);

            await RemovePolicyCachesAsync(ct);
            return true;
        }

        private async Task EnsureNoOverlapAsync(PayrollPolicyType policyType, string code, DateTime from, DateTime? to, int? excludeId, CancellationToken ct)
        {
            var samePolicies = await _policyRepo.GetByTypeAndCodeAsync(policyType, code, ct);
            var newFrom = from.Date;
            var newTo = to?.Date ?? DateTime.MaxValue.Date;

            var hasOverlap = samePolicies
                .Where(x => !excludeId.HasValue || x.Id != excludeId.Value)
                .Any(x =>
                {
                    var existingFrom = x.EffectiveFrom.Date;
                    var existingTo = x.EffectiveTo?.Date ?? DateTime.MaxValue.Date;
                    return newFrom <= existingTo && existingFrom <= newTo;
                });

            if (hasOverlap)
                throw new ArgumentException("Khoang hieu luc cua chinh sach bi trung voi phiên bản dang ton tai.");
        }

        private static void Normalize(CreatePayrollPolicyDto dto)
        {
            dto.Code = dto.Code.Trim().ToUpperInvariant();
        }

        private static void Validate(CreatePayrollPolicyDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Code))
                throw new ArgumentException("Mã chính sách không được để trống.");
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("Tên chính sách không được để trống.");
            if (dto.EffectiveFrom == default)
                throw new ArgumentException("Ngày hiệu lực không hợp lệ.");
            if (dto.EffectiveTo.HasValue && dto.EffectiveTo.Value.Date < dto.EffectiveFrom.Date)
                throw new ArgumentException("Ngay het hieu luc phai lon hon ngay hieu luc.");
            if (dto.Status == PolicyVersionStatus.Draft && dto.IsActive)
                dto.IsActive = false;

            switch (dto.ValueType)
            {
                case PayrollPolicyValueType.RatePercent:
                    if (!dto.RatePercent.HasValue || dto.RatePercent.Value < 0)
                        throw new ArgumentException("Chinh sach ty le phai co RatePercent >= 0.");
                    break;
                case PayrollPolicyValueType.Amount:
                    if (!dto.Amount.HasValue || dto.Amount.Value < 0)
                        throw new ArgumentException("Chinh sach so tien phai co Amount >= 0.");
                    break;
                case PayrollPolicyValueType.Bracket:
                    if (!dto.FromAmount.HasValue || dto.FromAmount.Value < 0)
                        throw new ArgumentException("Bac thue/khoang luong phai co FromAmount >= 0.");
                    if (dto.ToAmount.HasValue && dto.ToAmount.Value <= dto.FromAmount.Value)
                        throw new ArgumentException("ToAmount phai lon hon FromAmount.");
                    if (!dto.RatePercent.HasValue || dto.RatePercent.Value < 0)
                        throw new ArgumentException("Bac thue/khoang luong phai co RatePercent >= 0.");
                    break;
                case PayrollPolicyValueType.Formula:
                    if (string.IsNullOrWhiteSpace(dto.FormulaJson))
                        throw new ArgumentException("Chinh sach cong thuc phai co FormulaJson.");
                    break;
            }
        }

        private async Task RemovePolicyCachesAsync(CancellationToken ct)
        {
            var types = Enum.GetValues<PayrollPolicyType>().Select(x => x.ToString()).Append("All");
            foreach (var type in types)
            {
                await _cache.RemoveAsync($"{CachePrefix}_{type}_False", ct);
                await _cache.RemoveAsync($"{CachePrefix}_{type}_True", ct);
            }
        }

        private static string BuildLockKey(PayrollPolicyType policyType, string code) =>
            $"payroll_policy_{policyType}_{code.Trim().ToUpperInvariant()}";

        private static string BuildVersionCode(string code, int version, string? explicitVersionCode)
        {
            return string.IsNullOrWhiteSpace(explicitVersionCode)
                ? $"{code.Trim().ToUpperInvariant()}_V{version}"
                : explicitVersionCode.Trim().ToUpperInvariant();
        }

        private static PayrollPolicyDto MapToDto(PayrollPolicy entity) => new()
        {
            Id = entity.Id,
            PolicyType = entity.PolicyType,
            Code = entity.Code,
            Name = entity.Name,
            ValueType = entity.ValueType,
            RatePercent = entity.RatePercent,
            Amount = entity.Amount,
            FromAmount = entity.FromAmount,
            ToAmount = entity.ToAmount,
            QuickDeduction = entity.QuickDeduction,
            FormulaJson = entity.FormulaJson,
            EffectiveFrom = entity.EffectiveFrom,
            EffectiveTo = entity.EffectiveTo,
            Version = entity.Version,
            VersionCode = entity.VersionCode,
            Status = entity.Status,
            SourceRef = entity.SourceRef,
            SupersedesVersionId = entity.SupersedesVersionId,
            ActivatedAt = entity.ActivatedAt,
            LockedAfterUsed = entity.LockedAfterUsed,
            IsActive = entity.IsActive,
            Description = entity.Description
        };
    }
}

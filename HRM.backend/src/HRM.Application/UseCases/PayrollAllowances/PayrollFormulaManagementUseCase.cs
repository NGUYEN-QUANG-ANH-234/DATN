using System.Text.RegularExpressions;
using HRM.backend.src.HRM.Application.DTOs.PayrollAllowances;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.PayrollAllowances.Usecases;
using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using HRM.backend.src.HRM.Application.Services.System;
using HRM.backend.src.HRM.Core.Entities.PayrollAllowances;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.PayrollAllowances;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;

namespace HRM.backend.src.HRM.Application.UseCases.PayrollAllowances
{
    public class PayrollFormulaManagementUseCase : IPayrollFormulaManagementUseCase
    {
        private static readonly Regex CodeRegex = new(@"^[A-Z0-9_]{2,80}$", RegexOptions.Compiled);
        private static readonly Regex IdentifierRegex = new(@"\b[A-Za-z_][A-Za-z0-9_]*\b", RegexOptions.Compiled);
        private static readonly string[] ForbiddenTokens =
        {
            ";", "\"", "'", "[", "]", "{", "}", "=>", "new ", "typeof", "System", "DateTime", "File", "Process", "Environment"
        };

        private static readonly HashSet<string> AllowedFunctions = new(StringComparer.OrdinalIgnoreCase)
        {
            "min", "max", "round", "abs", "pit"
        };

        private static readonly string[] BuiltInVariables =
        {
            "gross_income", "insurance_salary", "employee_insurance_amount", "employer_contribution_amount",
            "taxable_gross_income", "taxable_income", "pit_tax_base", "pit_amount", "other_deductions",
            "net_salary", "component_base_salary_actual", "component_kpi_bonus", "component_project_bonus"
        };

        private readonly IPayrollFormulaRepository _formulaRepo;
        private readonly ISalaryVariableUseCase _salaryVariableUseCase;
        private readonly ISourceCatalogRepository _sourceCatalogRepo;
        private readonly IAuditLogRepository _auditRepo;
        private readonly ILockService _lockService;
        private readonly IUnitOfWork _unitOfWork;

        public PayrollFormulaManagementUseCase(
            IPayrollFormulaRepository formulaRepo,
            ISalaryVariableUseCase salaryVariableUseCase,
            ISourceCatalogRepository sourceCatalogRepo,
            IAuditLogRepository auditRepo,
            ILockService lockService,
            IUnitOfWork unitOfWork)
        {
            _formulaRepo = formulaRepo;
            _salaryVariableUseCase = salaryVariableUseCase;
            _sourceCatalogRepo = sourceCatalogRepo;
            _auditRepo = auditRepo;
            _lockService = lockService;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<PayrollFormulaDto>> GetListAsync(FormulaStatus? status, string actorRole, CancellationToken ct = default)
        {
            EnsureViewer(actorRole);
            var formulas = await _formulaRepo.GetListAsync(status, ct);
            return formulas.Select(Map).ToList();
        }

        public async Task<PayrollFormulaDto> GetDetailAsync(int id, string actorRole, CancellationToken ct = default)
        {
            EnsureViewer(actorRole);
            var formula = await _formulaRepo.GetDetailAsync(id, ct)
                ?? throw new KeyNotFoundException("Khong tim thay cong thuc luong.");
            return Map(formula);
        }

        public async Task<List<PayrollFormulaVariableDto>> GetVariablesAsync(string actorRole, CancellationToken ct = default)
        {
            EnsureViewer(actorRole);
            var variables = (await _salaryVariableUseCase.GetAllVariablesAsync(ct)).ToList();
            var sources = await _sourceCatalogRepo.GetOrderedCatalogsAsync(variables.Select(v => v.Source), ct);
            var sourceByPath = sources
                .GroupBy(s => s.SourcePath, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            return variables
                .OrderByDescending(v => v.IsActive)
                .ThenBy(v => v.Code)
                .Select(v =>
                {
                    sourceByPath.TryGetValue(v.Source, out var source);
                    return new PayrollFormulaVariableDto
                    {
                        Code = v.Code,
                        Source = v.Source,
                        Description = v.Description ?? source?.DisplayName ?? v.Source,
                        Module = source?.Module,
                        DataType = source?.DataType.ToString(),
                        AggregationType = source?.AggregationType.ToString(),
                        IsPeriodBased = source?.IsPeriodBased ?? false,
                        IsActive = v.IsActive && (source?.IsActive ?? true)
                    };
                })
                .ToList();
        }

        public async Task<PayrollFormulaValidationResultDto> ValidateAsync(UpsertPayrollFormulaDto dto, string actorRole, CancellationToken ct = default)
        {
            EnsureViewer(actorRole);
            return await ValidateInternalAsync(dto, ct);
        }

        public async Task<PayrollFormulaDto> CreateDraftAsync(UpsertPayrollFormulaDto dto, int actorAccountId, string actorRole, CancellationToken ct = default)
        {
            EnsureFormulaManager(actorRole);
            var code = NormalizeCode(dto.FormulaCode);
            return await _lockService.GetWithLockAsync(
                LockKeys.PayrollFormulaCode(code),
                innerCt => CreateDraftCoreAsync(dto, actorAccountId, actorRole, innerCt),
                TimeSpan.FromSeconds(20),
                ct);
        }

        private async Task<PayrollFormulaDto> CreateDraftCoreAsync(UpsertPayrollFormulaDto dto, int actorAccountId, string actorRole, CancellationToken ct = default)
        {
            EnsureFormulaManager(actorRole);
            var validation = await ValidateInternalAsync(dto, ct);
            ThrowIfInvalid(validation);

            var code = NormalizeCode(dto.FormulaCode);
            var formula = new PayrollFormula
            {
                FormulaCode = code,
                FormulaName = dto.FormulaName.Trim(),
                Expression = TrimToNull(dto.Expression),
                IsActive = false,
                ContractType = dto.ContractType,
                PayBasis = dto.PayBasis,
                EmployeeType = dto.EmployeeType,
                DeptId = PositiveOrNull(dto.DeptId),
                PositionId = PositiveOrNull(dto.PositionId),
                JobLevelId = PositiveOrNull(dto.JobLevelId),
                Version = await _formulaRepo.GetNextVersionAsync(code, ct),
                VersionCode = BuildVersionCode(code, dto.VersionCode, null),
                EffectiveFrom = dto.EffectiveFrom.Date,
                EffectiveTo = dto.EffectiveTo?.Date,
                Status = FormulaStatus.Draft,
                CreatedByAccountId = actorAccountId,
                CreatedAt = DateTime.UtcNow
            };
            formula.VersionCode = BuildVersionCode(code, dto.VersionCode, formula.Version);
            ApplyLines(formula, dto.Lines);

            await _formulaRepo.AddAsync(formula, ct);
            await _auditRepo.LogSystemEventAsync("PAYROLL_FORMULA_DRAFT_CREATED", actorAccountId, "payroll_formulas", $"{formula.FormulaCode}:v{formula.Version}");
            await _unitOfWork.CommitAsync(ct);
            return Map(formula);
        }

        public async Task<PayrollFormulaDto> UpdateDraftAsync(int id, UpsertPayrollFormulaDto dto, int actorAccountId, string actorRole, CancellationToken ct = default)
        {
            EnsureFormulaManager(actorRole);
            return await _lockService.GetWithLockAsync(
                LockKeys.PayrollFormula(id),
                innerCt => UpdateDraftCoreAsync(id, dto, actorAccountId, actorRole, innerCt),
                TimeSpan.FromSeconds(20),
                ct);
        }

        private async Task<PayrollFormulaDto> UpdateDraftCoreAsync(int id, UpsertPayrollFormulaDto dto, int actorAccountId, string actorRole, CancellationToken ct = default)
        {
            EnsureFormulaManager(actorRole);
            var formula = await _formulaRepo.GetTrackedDetailAsync(id, ct)
                ?? throw new KeyNotFoundException("Khong tim thay cong thuc luong.");
            if (formula.Status != FormulaStatus.Draft && formula.Status != FormulaStatus.RevisionRequired)
                throw new InvalidOperationException("Chi co the sua cong thuc dang nhap hoac can chinh sua.");

            var validation = await ValidateInternalAsync(dto, ct);
            ThrowIfInvalid(validation);

            var normalizedCode = NormalizeCode(dto.FormulaCode);
            if (!string.Equals(normalizedCode, formula.FormulaCode, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Khong the doi ma cong thuc sau khi da tao. Hay clone version hoac tao cong thuc moi neu can ma khac.");

            formula.FormulaName = dto.FormulaName.Trim();
            formula.Expression = TrimToNull(dto.Expression);
            formula.ContractType = dto.ContractType;
            formula.PayBasis = dto.PayBasis;
            formula.EmployeeType = dto.EmployeeType;
            formula.DeptId = PositiveOrNull(dto.DeptId);
            formula.PositionId = PositiveOrNull(dto.PositionId);
            formula.JobLevelId = PositiveOrNull(dto.JobLevelId);
            formula.VersionCode = BuildVersionCode(formula.FormulaCode, dto.VersionCode, formula.Version);
            formula.EffectiveFrom = dto.EffectiveFrom.Date;
            formula.EffectiveTo = dto.EffectiveTo?.Date;
            formula.UpdatedAt = DateTime.UtcNow;
            formula.RejectReason = null;
            ApplyLines(formula, dto.Lines);

            _formulaRepo.Update(formula);
            await _auditRepo.LogSystemEventAsync("PAYROLL_FORMULA_DRAFT_UPDATED", actorAccountId, "payroll_formulas", $"{formula.FormulaCode}:v{formula.Version}");
            await _unitOfWork.CommitAsync(ct);
            return Map(formula);
        }

        public async Task<PayrollFormulaDto> CloneVersionAsync(int id, int actorAccountId, string actorRole, CancellationToken ct = default)
        {
            EnsureFormulaManager(actorRole);
            var source = await _formulaRepo.GetDetailAsync(id, ct)
                ?? throw new KeyNotFoundException("Khong tim thay cong thuc luong.");
            return await _lockService.GetWithLockAsync(
                LockKeys.PayrollFormulaCode(source.FormulaCode),
                innerCt => CloneVersionCoreAsync(id, actorAccountId, actorRole, innerCt),
                TimeSpan.FromSeconds(20),
                ct);
        }

        private async Task<PayrollFormulaDto> CloneVersionCoreAsync(int id, int actorAccountId, string actorRole, CancellationToken ct = default)
        {
            EnsureFormulaManager(actorRole);
            var source = await _formulaRepo.GetDetailAsync(id, ct)
                ?? throw new KeyNotFoundException("Khong tim thay cong thuc luong.");
            var nextVersion = await _formulaRepo.GetNextVersionAsync(source.FormulaCode, ct);
            var clone = new PayrollFormula
            {
                FormulaCode = source.FormulaCode,
                FormulaName = $"{source.FormulaName} - v{nextVersion}",
                Expression = source.Expression,
                IsActive = false,
                ContractType = source.ContractType,
                PayBasis = source.PayBasis,
                EmployeeType = source.EmployeeType,
                DeptId = source.DeptId,
                PositionId = source.PositionId,
                JobLevelId = source.JobLevelId,
                Version = nextVersion,
                VersionCode = BuildVersionCode(source.FormulaCode, null, nextVersion),
                EffectiveFrom = DateTime.UtcNow.Date,
                EffectiveTo = source.EffectiveTo,
                Status = FormulaStatus.Draft,
                CreatedByAccountId = actorAccountId,
                CreatedAt = DateTime.UtcNow
            };
            foreach (var line in source.Lines.OrderBy(l => l.CalculationOrder))
            {
                clone.Lines.Add(new PayrollFormulaLine
                {
                    SalaryComponentTypeId = line.SalaryComponentTypeId,
                    ComponentCode = NormalizeCode(line.ComponentCode),
                    Expression = line.Expression,
                    CalculationOrder = line.CalculationOrder,
                    IsGrossComponent = line.IsGrossComponent,
                    IsTaxable = line.IsTaxable,
                    IsInsuranceBased = line.IsInsuranceBased,
                    IsDeduction = line.IsDeduction,
                    IsSnapshotRequired = line.IsSnapshotRequired,
                    Note = line.Note,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _formulaRepo.AddAsync(clone, ct);
            await _auditRepo.LogSystemEventAsync("PAYROLL_FORMULA_VERSION_CLONED", actorAccountId, "payroll_formulas", $"{source.FormulaCode}:v{source.Version}->v{nextVersion}");
            await _unitOfWork.CommitAsync(ct);
            return Map(clone);
        }

        public async Task<PayrollFormulaDto> SubmitForApprovalAsync(int id, int actorAccountId, string actorRole, CancellationToken ct = default)
        {
            EnsureFormulaManager(actorRole);
            return await _lockService.GetWithLockAsync(
                LockKeys.PayrollFormula(id),
                innerCt => SubmitForApprovalCoreAsync(id, actorAccountId, actorRole, innerCt),
                TimeSpan.FromSeconds(20),
                ct);
        }

        private async Task<PayrollFormulaDto> SubmitForApprovalCoreAsync(int id, int actorAccountId, string actorRole, CancellationToken ct = default)
        {
            EnsureFormulaManager(actorRole);
            var formula = await _formulaRepo.GetTrackedDetailAsync(id, ct)
                ?? throw new KeyNotFoundException("Khong tim thay cong thuc luong.");
            if (formula.Status != FormulaStatus.Draft && formula.Status != FormulaStatus.RevisionRequired)
                throw new InvalidOperationException("Chi cong thuc nhap hoac can chinh sua moi duoc gui duyet.");

            var validation = await ValidateInternalAsync(ToUpsert(formula), ct);
            ThrowIfInvalid(validation);

            formula.Status = FormulaStatus.PendingDirectorApproval;
            formula.SubmittedByAccountId = actorAccountId;
            formula.SubmittedAt = DateTime.UtcNow;
            formula.DeadlineAt = DateTime.UtcNow.AddDays(3);
            formula.UpdatedAt = DateTime.UtcNow;
            formula.RejectReason = null;
            _formulaRepo.Update(formula);

            await _auditRepo.LogSystemEventAsync("PAYROLL_FORMULA_SUBMITTED", actorAccountId, "payroll_formulas", $"{formula.FormulaCode}:v{formula.Version}");
            await _unitOfWork.CommitAsync(ct);
            return Map(formula);
        }

        public async Task<PayrollFormulaDto> DirectorReviewAsync(int id, PayrollFormulaReviewDto dto, int actorAccountId, string actorRole, CancellationToken ct = default)
        {
            EnsureDirector(actorRole);
            return await _lockService.GetWithLockAsync(
                LockKeys.PayrollFormula(id),
                innerCt => DirectorReviewCoreAsync(id, dto, actorAccountId, actorRole, innerCt),
                TimeSpan.FromSeconds(20),
                ct);
        }

        private async Task<PayrollFormulaDto> DirectorReviewCoreAsync(int id, PayrollFormulaReviewDto dto, int actorAccountId, string actorRole, CancellationToken ct = default)
        {
            EnsureDirector(actorRole);
            var formula = await _formulaRepo.GetTrackedDetailAsync(id, ct)
                ?? throw new KeyNotFoundException("Khong tim thay cong thuc luong.");
            if (formula.Status != FormulaStatus.PendingDirectorApproval)
                throw new InvalidOperationException("Chi cong thuc dang cho giam doc duyet moi duoc xu ly.");

            formula.ApprovedByAccountId = actorAccountId;
            formula.ApprovedAt = dto.IsApproved ? DateTime.UtcNow : null;
            formula.ReviewNote = TrimToNull(dto.Note);
            formula.UpdatedAt = DateTime.UtcNow;

            if (dto.IsApproved)
            {
                formula.Status = FormulaStatus.Approved;
                formula.RejectReason = null;
            }
            else if (dto.RequestRevision)
            {
                formula.Status = FormulaStatus.RevisionRequired;
                formula.RejectReason = TrimToNull(dto.Note) ?? "Can chinh sua cong thuc truoc khi duyet.";
            }
            else
            {
                formula.Status = FormulaStatus.Rejected;
                formula.RejectReason = TrimToNull(dto.Note) ?? "Cong thuc khong duoc phe duyet.";
            }

            _formulaRepo.Update(formula);
            await _auditRepo.LogSystemEventAsync(
                dto.IsApproved ? "PAYROLL_FORMULA_APPROVED" : dto.RequestRevision ? "PAYROLL_FORMULA_REVISION_REQUESTED" : "PAYROLL_FORMULA_REJECTED",
                actorAccountId,
                "payroll_formulas",
                $"{formula.FormulaCode}:v{formula.Version}");
            await _unitOfWork.CommitAsync(ct);
            return Map(formula);
        }

        public async Task<PayrollFormulaDto> ActivateAsync(int id, int actorAccountId, string actorRole, CancellationToken ct = default)
        {
            EnsureFormulaManager(actorRole);
            return await _lockService.GetWithLockAsync(
                LockKeys.PayrollFormula(id),
                innerCt => ActivateCoreAsync(id, actorAccountId, actorRole, innerCt),
                TimeSpan.FromSeconds(20),
                ct);
        }

        private async Task<PayrollFormulaDto> ActivateCoreAsync(int id, int actorAccountId, string actorRole, CancellationToken ct = default)
        {
            EnsureFormulaManager(actorRole);
            var formula = await _formulaRepo.GetTrackedDetailAsync(id, ct)
                ?? throw new KeyNotFoundException("Khong tim thay cong thuc luong.");
            if (formula.Status != FormulaStatus.Approved && formula.Status != FormulaStatus.Active)
                throw new InvalidOperationException("Chi cong thuc da duyet moi duoc kich hoat.");

            var validation = await ValidateInternalAsync(ToUpsert(formula), ct);
            ThrowIfInvalid(validation);

            var overlaps = await _formulaRepo.GetOverlappingActiveAsync(formula, ct);
            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                foreach (var current in overlaps)
                {
                    current.IsActive = false;
                    current.Status = FormulaStatus.Archived;
                    current.ArchivedByAccountId = actorAccountId;
                    current.ArchivedAt = DateTime.UtcNow;
                    current.UpdatedAt = DateTime.UtcNow;
                    if (!current.EffectiveTo.HasValue || current.EffectiveTo.Value.Date >= formula.EffectiveFrom.Date)
                        current.EffectiveTo = formula.EffectiveFrom.Date.AddDays(-1);
                    _formulaRepo.Update(current);
                }

                formula.Status = FormulaStatus.Active;
                formula.IsActive = true;
                formula.ActivatedByAccountId = actorAccountId;
                formula.ActivatedAt = DateTime.UtcNow;
                formula.UpdatedAt = DateTime.UtcNow;
                _formulaRepo.Update(formula);

                await _auditRepo.LogSystemEventAsync("PAYROLL_FORMULA_ACTIVATED", actorAccountId, "payroll_formulas", $"{formula.FormulaCode}:v{formula.Version}, archived={overlaps.Count}");
                await _unitOfWork.CommitAsync(ct);
            }, ct);

            return Map(formula);
        }

        public async Task<PayrollFormulaDto> ArchiveAsync(int id, PayrollFormulaActionNoteDto dto, int actorAccountId, string actorRole, CancellationToken ct = default)
        {
            EnsureFormulaManager(actorRole);
            return await _lockService.GetWithLockAsync(
                LockKeys.PayrollFormula(id),
                innerCt => ArchiveCoreAsync(id, dto, actorAccountId, actorRole, innerCt),
                TimeSpan.FromSeconds(20),
                ct);
        }

        private async Task<PayrollFormulaDto> ArchiveCoreAsync(int id, PayrollFormulaActionNoteDto dto, int actorAccountId, string actorRole, CancellationToken ct = default)
        {
            EnsureFormulaManager(actorRole);
            var formula = await _formulaRepo.GetTrackedDetailAsync(id, ct)
                ?? throw new KeyNotFoundException("Khong tim thay cong thuc luong.");
            if (formula.Status == FormulaStatus.Archived || formula.Status == FormulaStatus.Expired)
                return Map(formula);

            formula.IsActive = false;
            formula.Status = FormulaStatus.Archived;
            formula.ArchivedByAccountId = actorAccountId;
            formula.ArchivedAt = DateTime.UtcNow;
            formula.ReviewNote = TrimToNull(dto.Note) ?? formula.ReviewNote;
            formula.UpdatedAt = DateTime.UtcNow;
            _formulaRepo.Update(formula);

            await _auditRepo.LogSystemEventAsync("PAYROLL_FORMULA_ARCHIVED", actorAccountId, "payroll_formulas", $"{formula.FormulaCode}:v{formula.Version}");
            await _unitOfWork.CommitAsync(ct);
            return Map(formula);
        }

        private async Task<PayrollFormulaValidationResultDto> ValidateInternalAsync(UpsertPayrollFormulaDto dto, CancellationToken ct)
        {
            var result = new PayrollFormulaValidationResultDto();
            var code = NormalizeCode(dto.FormulaCode);
            if (string.IsNullOrWhiteSpace(dto.FormulaName))
                result.Errors.Add("Can nhap ten cong thuc.");
            if (!CodeRegex.IsMatch(code))
                result.Errors.Add("Ma cong thuc chi duoc gom chu in hoa, so va dau gach duoi.");
            if (dto.EffectiveTo.HasValue && dto.EffectiveTo.Value.Date < dto.EffectiveFrom.Date)
                result.Errors.Add("Ngay het hieu luc phai lon hon hoac bang ngay bat dau.");
            if (dto.Lines.Count == 0)
                result.Errors.Add("Can co it nhat mot dong cong thuc.");

            var duplicateComponent = dto.Lines
                .Where(l => !string.IsNullOrWhiteSpace(l.ComponentCode))
                .GroupBy(l => NormalizeCode(l.ComponentCode), StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(g => g.Count() > 1);
            if (duplicateComponent != null)
                result.Errors.Add($"Trung ma khoan luong {duplicateComponent.Key}.");

            var duplicateOrder = dto.Lines
                .GroupBy(l => l.CalculationOrder)
                .FirstOrDefault(g => g.Count() > 1);
            if (duplicateOrder != null)
                result.Warnings.Add($"Co nhieu dong cung thu tu {duplicateOrder.Key}; he thong se sap xep them theo Id.");

            var variables = await GetVariablesAsync("Admin", ct);
            var allowedIdentifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var variable in variables.Where(v => v.IsActive))
            {
                allowedIdentifiers.Add(variable.Source);
                allowedIdentifiers.Add(variable.Code);
                allowedIdentifiers.Add(variable.Code.ToLowerInvariant());
            }
            foreach (var fn in AllowedFunctions) allowedIdentifiers.Add(fn);
            foreach (var builtIn in BuiltInVariables) allowedIdentifiers.Add(builtIn);
            foreach (var line in dto.Lines)
            {
                var lineCode = NormalizeCode(line.ComponentCode);
                if (string.IsNullOrWhiteSpace(lineCode))
                    result.Errors.Add("Dong cong thuc thieu ma khoan luong.");
                else if (!CodeRegex.IsMatch(lineCode))
                    result.Errors.Add($"Ma khoan luong {line.ComponentCode} khong hop le.");
                else
                {
                    allowedIdentifiers.Add(SafeVariableName(lineCode));
                    allowedIdentifiers.Add($"component_{SafeVariableName(lineCode)}");
                }

                if (string.IsNullOrWhiteSpace(line.Expression))
                    result.Errors.Add($"Dong {line.ComponentCode} thieu bieu thuc tinh.");
                else if (line.Expression.Length > 1000)
                    result.Errors.Add($"Bieu thuc dong {line.ComponentCode} qua dai.");
            }

            foreach (var line in dto.Lines)
            {
                if (string.IsNullOrWhiteSpace(line.Expression)) continue;
                foreach (var token in ForbiddenTokens)
                {
                    if (line.Expression.Contains(token, StringComparison.OrdinalIgnoreCase))
                        result.Errors.Add($"Dong {line.ComponentCode} co token khong duoc phep: {token.Trim()}.");
                }

                var identifiers = IdentifierRegex.Matches(line.Expression)
                    .Select(m => m.Value)
                    .Where(v => !decimal.TryParse(v, out _))
                    .Where(v => !string.Equals(v, "true", StringComparison.OrdinalIgnoreCase))
                    .Where(v => !string.Equals(v, "false", StringComparison.OrdinalIgnoreCase))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var identifier in identifiers)
                {
                    if (!allowedIdentifiers.Contains(identifier))
                        result.Errors.Add($"Dong {line.ComponentCode} dung bien chua duoc phep: {identifier}.");
                }
            }

            return result;
        }

        private static void ThrowIfInvalid(PayrollFormulaValidationResultDto validation)
        {
            if (!validation.IsValid)
                throw new InvalidOperationException(string.Join(" ", validation.Errors));
        }

        private static void ApplyLines(PayrollFormula formula, IEnumerable<PayrollFormulaLineDto> lines)
        {
            formula.Lines.Clear();
            foreach (var line in lines.OrderBy(l => l.CalculationOrder))
            {
                formula.Lines.Add(new PayrollFormulaLine
                {
                    SalaryComponentTypeId = PositiveOrNull(line.SalaryComponentTypeId),
                    ComponentCode = NormalizeCode(line.ComponentCode),
                    Expression = line.Expression.Trim(),
                    CalculationOrder = line.CalculationOrder,
                    IsGrossComponent = line.IsGrossComponent,
                    IsTaxable = line.IsTaxable,
                    IsInsuranceBased = line.IsInsuranceBased,
                    IsDeduction = line.IsDeduction,
                    IsSnapshotRequired = line.IsSnapshotRequired,
                    Note = TrimToNull(line.Note),
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        private static UpsertPayrollFormulaDto ToUpsert(PayrollFormula formula)
        {
            return new UpsertPayrollFormulaDto
            {
                FormulaCode = formula.FormulaCode,
                FormulaName = formula.FormulaName,
                Expression = formula.Expression,
                ContractType = formula.ContractType,
                PayBasis = formula.PayBasis,
                EmployeeType = formula.EmployeeType,
                DeptId = formula.DeptId,
                PositionId = formula.PositionId,
                JobLevelId = formula.JobLevelId,
                VersionCode = formula.VersionCode,
                EffectiveFrom = formula.EffectiveFrom,
                EffectiveTo = formula.EffectiveTo,
                Lines = formula.Lines.Select(MapLine).ToList()
            };
        }

        private static PayrollFormulaDto Map(PayrollFormula formula)
        {
            return new PayrollFormulaDto
            {
                Id = formula.Id,
                FormulaCode = formula.FormulaCode,
                FormulaName = formula.FormulaName,
                Expression = formula.Expression,
                IsActive = formula.IsActive,
                ContractType = formula.ContractType,
                PayBasis = formula.PayBasis,
                EmployeeType = formula.EmployeeType,
                DeptId = formula.DeptId,
                PositionId = formula.PositionId,
                JobLevelId = formula.JobLevelId,
                Version = formula.Version,
                VersionCode = formula.VersionCode,
                EffectiveFrom = formula.EffectiveFrom,
                EffectiveTo = formula.EffectiveTo,
                Status = formula.Status,
                StatusText = ResolveStatusText(formula.Status),
                DeadlineAt = formula.DeadlineAt,
                CreatedByAccountId = formula.CreatedByAccountId,
                SubmittedByAccountId = formula.SubmittedByAccountId,
                SubmittedAt = formula.SubmittedAt,
                ApprovedByAccountId = formula.ApprovedByAccountId,
                ApprovedAt = formula.ApprovedAt,
                ActivatedByAccountId = formula.ActivatedByAccountId,
                ActivatedAt = formula.ActivatedAt,
                ArchivedByAccountId = formula.ArchivedByAccountId,
                ArchivedAt = formula.ArchivedAt,
                RejectReason = formula.RejectReason,
                ReviewNote = formula.ReviewNote,
                CreatedAt = formula.CreatedAt,
                UpdatedAt = formula.UpdatedAt,
                Lines = formula.Lines
                    .OrderBy(l => l.CalculationOrder)
                    .ThenBy(l => l.Id)
                    .Select(MapLine)
                    .ToList()
            };
        }

        private static PayrollFormulaLineDto MapLine(PayrollFormulaLine line)
        {
            return new PayrollFormulaLineDto
            {
                Id = line.Id,
                SalaryComponentTypeId = line.SalaryComponentTypeId,
                ComponentCode = line.ComponentCode,
                ComponentName = line.SalaryComponentType?.Name,
                Expression = line.Expression,
                CalculationOrder = line.CalculationOrder,
                IsGrossComponent = line.IsGrossComponent,
                IsTaxable = line.IsTaxable,
                IsInsuranceBased = line.IsInsuranceBased,
                IsDeduction = line.IsDeduction,
                IsSnapshotRequired = line.IsSnapshotRequired,
                Note = line.Note
            };
        }

        private static string ResolveStatusText(FormulaStatus status)
        {
            return status switch
            {
                FormulaStatus.Draft => "Ban nhap",
                FormulaStatus.Pending => "Cho duyet",
                FormulaStatus.PendingDirectorApproval => "Cho giam doc duyet",
                FormulaStatus.RevisionRequired => "Can chinh sua",
                FormulaStatus.Approved => "Da duyet",
                FormulaStatus.Active => "Dang ap dung",
                FormulaStatus.Rejected => "Tu choi",
                FormulaStatus.Archived => "Luu tru",
                FormulaStatus.Expired => "Het hieu luc",
                _ => status.ToString()
            };
        }

        private static string NormalizeCode(string? value)
        {
            return (value ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static string SafeVariableName(string value)
        {
            var chars = value.Trim().ToLowerInvariant().Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray();
            return new string(chars);
        }

        private static string BuildVersionCode(string formulaCode, string? requested, int? version)
        {
            if (!string.IsNullOrWhiteSpace(requested)) return requested.Trim();
            return version.HasValue ? $"{formulaCode}_V{version.Value}" : $"{formulaCode}_V";
        }

        private static int? PositiveOrNull(int? value) => value.HasValue && value.Value > 0 ? value.Value : null;

        private static string? TrimToNull(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static void EnsureViewer(string role)
        {
            if (IsAnyRole(role, "Admin", "HR", "Director", "Accountant", "Ke toan", "Kế toán")) return;
            throw new UnauthorizedAccessException("Ban khong co quyen xem cong thuc luong.");
        }

        private static void EnsureFormulaManager(string role)
        {
            if (IsAnyRole(role, "Admin", "HR", "Accountant", "Ke toan", "Kế toán")) return;
            throw new UnauthorizedAccessException("Ban khong co quyen quan tri cong thuc luong.");
        }

        private static void EnsureDirector(string role)
        {
            if (IsAnyRole(role, "Admin", "Director", "Giam doc", "Giám đốc")) return;
            throw new UnauthorizedAccessException("Chi Giam doc/Admin duoc duyet cong thuc luong.");
        }

        private static bool IsAnyRole(string role, params string[] accepted)
        {
            return accepted.Any(item => item.Equals(role, StringComparison.OrdinalIgnoreCase));
        }
    }
}

using System.Globalization;
using System.Text;
using HRM.backend.src.HRM.Application.DTOs.PayrollAllowances;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.PayrollAllowances.Usecases;
using HRM.backend.src.HRM.Application.Services.System;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.PayrollAllowances;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.PayrollAllowances;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using Microsoft.AspNetCore.Http;

namespace HRM.backend.src.HRM.Application.UseCases.PayrollAllowances
{
    public class ProjectBonusImportUseCase : IProjectBonusImportUseCase
    {
        private readonly IProjectBonusImportRepository _projectBonusRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IPayrollRepository _payrollRepo;
        private readonly IAuditLogRepository _auditRepo;
        private readonly ILockService _lockService;
        private readonly IUnitOfWork _unitOfWork;

        public ProjectBonusImportUseCase(
            IProjectBonusImportRepository projectBonusRepo,
            IEmployeeRepository employeeRepo,
            IPayrollRepository payrollRepo,
            IAuditLogRepository auditRepo,
            ILockService lockService,
            IUnitOfWork unitOfWork)
        {
            _projectBonusRepo = projectBonusRepo;
            _employeeRepo = employeeRepo;
            _payrollRepo = payrollRepo;
            _auditRepo = auditRepo;
            _lockService = lockService;
            _unitOfWork = unitOfWork;
        }

        public async Task<ProjectBonusImportPreviewDto> PreviewAsync(ProjectBonusImportRequestDto dto, int actorAccountId, string actorRole, CancellationToken ct = default)
        {
            EnsureImporter(actorRole);
            var result = await BuildValidatedImportAsync(dto, ct);
            return result.Preview;
        }

        public async Task<ProjectBonusImportBatchDto> ImportAsync(ProjectBonusImportRequestDto dto, int actorAccountId, string actorRole, CancellationToken ct = default)
        {
            EnsureImporter(actorRole);
            ValidatePeriod(dto.PeriodMonth, dto.PeriodYear);

            return await _lockService.GetWithLockAsync(
                LockKeys.ProjectBonusPeriod(dto.PeriodMonth, dto.PeriodYear),
                innerCt => ImportCoreAsync(dto, actorAccountId, actorRole, innerCt),
                TimeSpan.FromSeconds(30),
                ct);
        }

        private async Task<ProjectBonusImportBatchDto> ImportCoreAsync(ProjectBonusImportRequestDto dto, int actorAccountId, string actorRole, CancellationToken ct = default)
        {
            EnsureImporter(actorRole);
            var result = await BuildValidatedImportAsync(dto, ct);
            if (!result.Preview.CanSave)
                throw new ProjectBonusImportValidationException(result.Preview);

            var batch = new ProjectBonusImportBatch
            {
                PeriodMonth = dto.PeriodMonth,
                PeriodYear = dto.PeriodYear,
                PayrollPeriod = FormatPeriod(dto.PeriodMonth, dto.PeriodYear),
                FileName = dto.File.FileName,
                UploadedByAccountId = actorAccountId,
                Status = ProjectBonusImportStatus.Draft,
                TotalRows = result.ValidRows.Count,
                ValidRows = result.ValidRows.Count,
                ErrorRows = 0,
                TotalAmount = result.ValidRows.Sum(r => r.BonusAmount),
                CreatedAt = DateTime.UtcNow,
                Note = dto.Note
            };

            foreach (var row in result.ValidRows)
            {
                batch.Lines.Add(new ProjectBonusImportLine
                {
                    RowNumber = row.RowNumber,
                    EmployeeId = row.Employee!.Id,
                    EmployeeCodeSnapshot = row.Employee.EmployeeCode,
                    EmployeeNameSnapshot = row.Employee.FullName,
                    ProjectCode = row.ProjectCode,
                    ProjectName = row.ProjectName,
                    BonusAmount = row.BonusAmount,
                    Taxable = row.Taxable,
                    InsuranceContributable = row.InsuranceContributable,
                    Reason = row.Reason,
                    Note = row.Note,
                    ValidationStatus = ProjectBonusLineValidationStatus.Valid
                });
            }

            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                if (dto.Overwrite)
                    await RemoveDuplicateLinesForOverwriteAsync(result.ValidRows, dto.PeriodMonth, dto.PeriodYear, actorAccountId, ct);

                await _projectBonusRepo.AddAsync(batch, ct);
                await _auditRepo.LogSystemEventAsync(
                    "PROJECT_BONUS_IMPORT_CREATED",
                    actorAccountId,
                    "project_bonus_import_batches",
                    $"Imported project bonus draft for {batch.PayrollPeriod}. Rows={batch.ValidRows}, Amount={batch.TotalAmount}.");
                await _unitOfWork.CommitAsync(ct);
            }, ct);

            return MapBatch(batch);
        }

        public async Task<List<ProjectBonusImportBatchDto>> GetBatchesAsync(byte? month, short? year, ProjectBonusImportStatus? status, string actorRole, CancellationToken ct = default)
        {
            EnsureViewer(actorRole);
            var batches = await _projectBonusRepo.GetBatchesAsync(month, year, status, ct);
            return batches.Select(MapBatch).ToList();
        }

        public async Task<ProjectBonusImportBatchDto> GetDetailAsync(int id, string actorRole, CancellationToken ct = default)
        {
            EnsureViewer(actorRole);
            var batch = await _projectBonusRepo.GetDetailAsync(id, ct)
                ?? throw new KeyNotFoundException("Không tìm thấy batch thưởng dự án.");
            return MapBatch(batch);
        }

        public async Task<ProjectBonusImportBatchDto> SubmitAsync(int id, int actorAccountId, string actorRole, CancellationToken ct = default)
        {
            EnsureImporter(actorRole);
            return await _lockService.GetWithLockAsync(
                LockKeys.ProjectBonusBatch(id),
                innerCt => SubmitCoreAsync(id, actorAccountId, actorRole, innerCt),
                TimeSpan.FromSeconds(20),
                ct);
        }

        private async Task<ProjectBonusImportBatchDto> SubmitCoreAsync(int id, int actorAccountId, string actorRole, CancellationToken ct = default)
        {
            EnsureImporter(actorRole);
            var batch = await _projectBonusRepo.GetTrackedDetailAsync(id, ct)
                ?? throw new KeyNotFoundException("Không tìm thấy batch thưởng dự án.");

            if (batch.Status != ProjectBonusImportStatus.Draft)
                throw new InvalidOperationException("Chỉ batch ở trạng thái nháp mới được gửi duyệt.");
            if (batch.ErrorRows > 0 || batch.Lines.Any(l => l.ValidationStatus != ProjectBonusLineValidationStatus.Valid))
                throw new InvalidOperationException("Batch còn dòng lỗi, không thể gửi duyệt.");
            if (await _payrollRepo.HasLockedPayrollAsync(batch.PeriodMonth, batch.PeriodYear, ct))
                throw new InvalidOperationException("Kỳ lương đã khóa/chốt, không thể gửi duyệt thưởng dự án.");

            batch.Status = ProjectBonusImportStatus.PendingReview;
            _projectBonusRepo.Update(batch);
            await _auditRepo.LogSystemEventAsync(
                "PROJECT_BONUS_IMPORT_SUBMITTED",
                actorAccountId,
                "project_bonus_import_batches",
                $"Submitted project bonus import #{batch.Id} for director review.");
            await _unitOfWork.CommitAsync(ct);
            return MapBatch(batch);
        }

        public async Task<ProjectBonusImportBatchDto> DirectorReviewAsync(int id, ReviewProjectBonusImportDto dto, int actorAccountId, string actorRole, CancellationToken ct = default)
        {
            EnsureDirector(actorRole);
            return await _lockService.GetWithLockAsync(
                LockKeys.ProjectBonusBatch(id),
                innerCt => DirectorReviewCoreAsync(id, dto, actorAccountId, actorRole, innerCt),
                TimeSpan.FromSeconds(20),
                ct);
        }

        private async Task<ProjectBonusImportBatchDto> DirectorReviewCoreAsync(int id, ReviewProjectBonusImportDto dto, int actorAccountId, string actorRole, CancellationToken ct = default)
        {
            EnsureDirector(actorRole);
            var batch = await _projectBonusRepo.GetTrackedDetailAsync(id, ct)
                ?? throw new KeyNotFoundException("Không tìm thấy batch thưởng dự án.");

            if (batch.Status != ProjectBonusImportStatus.PendingReview)
                throw new InvalidOperationException("Chỉ batch đang chờ duyệt mới được xử lý.");
            if (await _payrollRepo.HasLockedPayrollAsync(batch.PeriodMonth, batch.PeriodYear, ct))
                throw new InvalidOperationException("Kỳ lương đã khóa/chốt, không thể duyệt thưởng dự án.");

            batch.Status = dto.IsApproved ? ProjectBonusImportStatus.Approved : ProjectBonusImportStatus.Rejected;
            batch.ApprovedByAccountId = actorAccountId;
            batch.ApprovedAt = DateTime.UtcNow;
            batch.Note = MergeNote(batch.Note, dto.Note);
            _projectBonusRepo.Update(batch);

            await _auditRepo.LogSystemEventAsync(
                dto.IsApproved ? "PROJECT_BONUS_IMPORT_APPROVED" : "PROJECT_BONUS_IMPORT_REJECTED",
                actorAccountId,
                "project_bonus_import_batches",
                $"Director reviewed project bonus import #{batch.Id}. Approved={dto.IsApproved}.");
            await _unitOfWork.CommitAsync(ct);
            return MapBatch(batch);
        }

        public async Task<ProjectBonusImportBatchDto> CancelAsync(int id, int actorAccountId, string actorRole, string? note, CancellationToken ct = default)
        {
            EnsureImporter(actorRole);
            return await _lockService.GetWithLockAsync(
                LockKeys.ProjectBonusBatch(id),
                innerCt => CancelCoreAsync(id, actorAccountId, actorRole, note, innerCt),
                TimeSpan.FromSeconds(20),
                ct);
        }

        private async Task<ProjectBonusImportBatchDto> CancelCoreAsync(int id, int actorAccountId, string actorRole, string? note, CancellationToken ct = default)
        {
            EnsureImporter(actorRole);
            var batch = await _projectBonusRepo.GetTrackedDetailAsync(id, ct)
                ?? throw new KeyNotFoundException("Không tìm thấy batch thưởng dự án.");

            if (batch.Status == ProjectBonusImportStatus.Approved)
                throw new InvalidOperationException("Batch đã duyệt không được sửa hoặc hủy. Hãy tạo batch điều chỉnh/ghi đè mới nếu cần thay đổi.");
            if (batch.Status == ProjectBonusImportStatus.Cancelled)
                return MapBatch(batch);

            batch.Status = ProjectBonusImportStatus.Cancelled;
            batch.Note = MergeNote(batch.Note, note);
            _projectBonusRepo.Update(batch);

            await _auditRepo.LogSystemEventAsync(
                "PROJECT_BONUS_IMPORT_CANCELLED",
                actorAccountId,
                "project_bonus_import_batches",
                $"Cancelled project bonus import #{batch.Id} for {batch.PayrollPeriod}.");
            await _unitOfWork.CommitAsync(ct);
            return MapBatch(batch);
        }

        public async Task<List<ProjectBonusImportBatchDto>> GetPendingDirectorAsync(int actorAccountId, string actorRole, CancellationToken ct = default)
        {
            EnsureDirector(actorRole);
            var batches = await _projectBonusRepo.GetBatchesAsync(null, null, ProjectBonusImportStatus.PendingReview, ct);
            return batches.Select(MapBatch).ToList();
        }

        private async Task<ValidatedProjectBonusImport> BuildValidatedImportAsync(ProjectBonusImportRequestDto dto, CancellationToken ct)
        {
            ValidatePeriod(dto.PeriodMonth, dto.PeriodYear);
            ValidateFile(dto.File);

            var parsedRows = await ParseCsvAsync(dto.File, ct);
            var globalErrors = new List<string>();
            if (await _payrollRepo.HasLockedPayrollAsync(dto.PeriodMonth, dto.PeriodYear, ct))
                globalErrors.Add("Kỳ lương đã khóa/chốt, không thể import thưởng dự án.");

            var employeeCodes = parsedRows
                .Where(r => !string.IsNullOrWhiteSpace(r.EmployeeCode))
                .Select(r => r.EmployeeCode.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var employees = employeeCodes.Count == 0
                ? new List<Employee>()
                : (await _employeeRepo.FindAsync(e => employeeCodes.Contains(e.EmployeeCode), ct)).ToList();
            var employeeByCode = employees
                .GroupBy(e => e.EmployeeCode.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var duplicateCandidates = await _projectBonusRepo.GetDuplicateCandidatesAsync(dto.PeriodMonth, dto.PeriodYear, ct);
            var existingKeys = duplicateCandidates
                .Select(l => BuildKey(l.EmployeeCodeSnapshot, l.ProjectCode))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var duplicateRowsInFile = parsedRows
                .Where(r => !string.IsNullOrWhiteSpace(r.EmployeeCode) && !string.IsNullOrWhiteSpace(r.ProjectCode))
                .GroupBy(r => BuildKey(r.EmployeeCode, r.ProjectCode), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var validatedRows = new List<ValidatedProjectBonusRow>();
            var lineDtos = new List<ProjectBonusImportLineDto>();

            foreach (var row in parsedRows)
            {
                var errors = new List<string>(row.ParseErrors);
                Employee? employee = null;

                if (string.IsNullOrWhiteSpace(row.EmployeeCode))
                {
                    errors.Add("Thiếu mã nhân viên.");
                }
                else if (!employeeByCode.TryGetValue(row.EmployeeCode.Trim(), out employee))
                {
                    errors.Add($"Mã nhân viên {row.EmployeeCode} không tồn tại.");
                }
                else if (!IsEligibleEmployee(employee))
                {
                    errors.Add($"Nhân viên {employee.EmployeeCode} đang ở trạng thái {employee.Status}, không hợp lệ để nhận thưởng kỳ này.");
                }

                if (!string.IsNullOrWhiteSpace(row.PayrollPeriod))
                {
                    if (!TryParsePeriod(row.PayrollPeriod, out var rowMonth, out var rowYear))
                        errors.Add("Kỳ lương trong file không hợp lệ. Định dạng gợi ý: MM/yyyy.");
                    else if (rowMonth != dto.PeriodMonth || rowYear != dto.PeriodYear)
                        errors.Add($"Kỳ lương trong dòng ({rowMonth:00}/{rowYear}) không khớp kỳ đang import ({dto.PeriodMonth:00}/{dto.PeriodYear}).");
                }

                if (string.IsNullOrWhiteSpace(row.ProjectCode))
                    errors.Add("Thiếu mã dự án.");
                if (string.IsNullOrWhiteSpace(row.ProjectName))
                    errors.Add("Thiếu tên dự án.");
                if (row.BonusAmount <= 0)
                    errors.Add("Số tiền thưởng phải lớn hơn 0.");

                var key = BuildKey(row.EmployeeCode, row.ProjectCode);
                if (duplicateRowsInFile.Contains(key))
                    errors.Add("Dòng thưởng bị trùng trong chính file import theo Mã nhân viên + Mã dự án.");
                if (!dto.Overwrite && existingKeys.Contains(key))
                    errors.Add("Dòng thưởng đã tồn tại trong hệ thống cho kỳ này. Bật chế độ ghi đè nếu cần thay thế.");

                var validationStatus = errors.Count == 0
                    ? ProjectBonusLineValidationStatus.Valid
                    : ProjectBonusLineValidationStatus.Invalid;
                var validated = new ValidatedProjectBonusRow
                {
                    RowNumber = row.RowNumber,
                    Employee = employee,
                    EmployeeCode = row.EmployeeCode.Trim(),
                    EmployeeName = employee?.FullName,
                    ProjectCode = row.ProjectCode.Trim(),
                    ProjectName = row.ProjectName.Trim(),
                    BonusAmount = row.BonusAmount,
                    Taxable = row.Taxable,
                    InsuranceContributable = row.InsuranceContributable,
                    Reason = row.Reason,
                    Note = row.Note,
                    ValidationStatus = validationStatus,
                    ErrorMessage = errors.Count == 0 ? null : string.Join("; ", errors)
                };
                validatedRows.Add(validated);
                lineDtos.Add(MapValidatedRow(validated));
            }

            if (parsedRows.Count == 0)
                globalErrors.Add("File không có dòng thưởng dự án hợp lệ.");

            var validRows = validatedRows
                .Where(r => r.ValidationStatus == ProjectBonusLineValidationStatus.Valid)
                .ToList();
            var preview = new ProjectBonusImportPreviewDto
            {
                PeriodMonth = dto.PeriodMonth,
                PeriodYear = dto.PeriodYear,
                PayrollPeriod = FormatPeriod(dto.PeriodMonth, dto.PeriodYear),
                FileName = dto.File.FileName,
                Overwrite = dto.Overwrite,
                TotalRows = parsedRows.Count,
                ValidRows = validRows.Count,
                ErrorRows = validatedRows.Count(r => r.ValidationStatus == ProjectBonusLineValidationStatus.Invalid) + globalErrors.Count,
                TotalAmount = validRows.Sum(r => r.BonusAmount),
                GlobalErrors = globalErrors,
                Lines = lineDtos
            };
            preview.CanSave = preview.ErrorRows == 0 && preview.TotalRows > 0;

            return new ValidatedProjectBonusImport
            {
                Preview = preview,
                ValidRows = validRows
            };
        }

        private async Task RemoveDuplicateLinesForOverwriteAsync(List<ValidatedProjectBonusRow> rows, byte month, short year, int actorAccountId, CancellationToken ct)
        {
            var keys = rows.Select(r => BuildKey(r.EmployeeCode, r.ProjectCode)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var candidates = await _projectBonusRepo.GetReplaceableDuplicateCandidatesAsync(month, year, ct);
            var duplicates = candidates
                .Where(l => keys.Contains(BuildKey(l.EmployeeCodeSnapshot, l.ProjectCode)))
                .ToList();

            if (duplicates.Count == 0) return;

            foreach (var group in duplicates.GroupBy(l => l.Batch))
            {
                var batch = group.Key;
                var removed = group.ToList();
                batch.TotalRows = Math.Max(0, batch.TotalRows - removed.Count);
                batch.ValidRows = Math.Max(0, batch.ValidRows - removed.Count(l => l.ValidationStatus == ProjectBonusLineValidationStatus.Valid));
                batch.ErrorRows = Math.Max(0, batch.ErrorRows - removed.Count(l => l.ValidationStatus == ProjectBonusLineValidationStatus.Invalid));
                batch.TotalAmount = Math.Max(0, batch.TotalAmount - removed.Where(l => l.ValidationStatus == ProjectBonusLineValidationStatus.Valid).Sum(l => l.BonusAmount));
                if (batch.TotalRows == 0)
                    batch.Status = ProjectBonusImportStatus.Cancelled;
                _projectBonusRepo.Update(batch);
            }

            _projectBonusRepo.RemoveLines(duplicates);
            await _auditRepo.LogSystemEventAsync(
                "PROJECT_BONUS_IMPORT_OVERWRITE",
                actorAccountId,
                "project_bonus_import_lines",
                $"Removed {duplicates.Count} duplicated project bonus lines before overwrite for {month:00}/{year}.");
        }

        private static async Task<List<ParsedProjectBonusRow>> ParseCsvAsync(IFormFile file, CancellationToken ct)
        {
            await using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var content = await reader.ReadToEndAsync(ct);
            var rawRows = content
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();
            if (rawRows.Count <= 1) return new List<ParsedProjectBonusRow>();

            var delimiter = ResolveDelimiter(rawRows[0]);
            var headers = SplitDelimitedLine(rawRows[0], delimiter);
            var headerIndex = headers
                .Select((header, index) => new { Key = NormalizeHeader(header), Index = index })
                .Where(x => !string.IsNullOrWhiteSpace(x.Key))
                .GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Index, StringComparer.OrdinalIgnoreCase);

            var rows = new List<ParsedProjectBonusRow>();
            for (var i = 1; i < rawRows.Count; i++)
            {
                var cells = SplitDelimitedLine(rawRows[i], delimiter);
                var amountText = GetCell(cells, headerIndex, 4, "SoTienThuong", "Số tiền thưởng", "BonusAmount", "Amount");
                var parseErrors = new List<string>();
                if (!TryParseMoney(amountText, out var bonusAmount))
                    parseErrors.Add("Số tiền thưởng không hợp lệ.");

                rows.Add(new ParsedProjectBonusRow
                {
                    RowNumber = i + 1,
                    EmployeeCode = GetCell(cells, headerIndex, 0, "MaNhanVien", "Mã nhân viên", "EmployeeCode"),
                    PayrollPeriod = GetCell(cells, headerIndex, 1, "KyLuong", "Kỳ lương", "PayrollPeriod", "Period"),
                    ProjectCode = GetCell(cells, headerIndex, 2, "MaDuAn", "Mã dự án", "ProjectCode"),
                    ProjectName = GetCell(cells, headerIndex, 3, "TenDuAn", "Tên dự án", "ProjectName"),
                    BonusAmount = bonusAmount,
                    Reason = EmptyToNull(GetCell(cells, headerIndex, 5, "LyDo", "Lý do", "Reason")),
                    Taxable = ParseBool(GetCell(cells, headerIndex, 6, "ChiuThueTNCN", "Chịu thuế TNCN", "Taxable"), defaultValue: true),
                    InsuranceContributable = ParseBool(GetCell(cells, headerIndex, 7, "TinhDongBaoHiem", "Tính đóng bảo hiểm", "InsuranceContributable"), defaultValue: false),
                    Note = EmptyToNull(GetCell(cells, headerIndex, 8, "GhiChu", "Ghi chú", "Note")),
                    ParseErrors = parseErrors
                });
            }

            return rows;
        }

        private static ProjectBonusImportLineDto MapValidatedRow(ValidatedProjectBonusRow row)
        {
            return new ProjectBonusImportLineDto
            {
                RowNumber = row.RowNumber,
                EmployeeId = row.Employee?.Id,
                EmployeeCode = row.EmployeeCode,
                EmployeeName = row.EmployeeName,
                ProjectCode = row.ProjectCode,
                ProjectName = row.ProjectName,
                BonusAmount = row.BonusAmount,
                Taxable = row.Taxable,
                InsuranceContributable = row.InsuranceContributable,
                Reason = row.Reason,
                Note = row.Note,
                ValidationStatus = row.ValidationStatus,
                ErrorMessage = row.ErrorMessage
            };
        }

        private static ProjectBonusImportBatchDto MapBatch(ProjectBonusImportBatch batch)
        {
            return new ProjectBonusImportBatchDto
            {
                Id = batch.Id,
                PeriodMonth = batch.PeriodMonth,
                PeriodYear = batch.PeriodYear,
                PayrollPeriod = batch.PayrollPeriod,
                FileName = batch.FileName,
                Status = batch.Status,
                StatusText = ResolveStatusText(batch.Status),
                TotalRows = batch.TotalRows,
                ValidRows = batch.ValidRows,
                ErrorRows = batch.ErrorRows,
                TotalAmount = batch.TotalAmount,
                UploadedByAccountId = batch.UploadedByAccountId,
                UploadedByName = batch.UploadedByAccount?.FullName ?? batch.UploadedByAccount?.Email,
                CreatedAt = batch.CreatedAt,
                ApprovedByAccountId = batch.ApprovedByAccountId,
                ApprovedByName = batch.ApprovedByAccount?.FullName ?? batch.ApprovedByAccount?.Email,
                ApprovedAt = batch.ApprovedAt,
                Note = batch.Note,
                Lines = batch.Lines
                    .OrderBy(l => l.RowNumber)
                    .Select(l => new ProjectBonusImportLineDto
                    {
                        Id = l.Id,
                        RowNumber = l.RowNumber,
                        EmployeeId = l.EmployeeId,
                        EmployeeCode = l.EmployeeCodeSnapshot,
                        EmployeeName = l.EmployeeNameSnapshot ?? l.Employee?.FullName,
                        ProjectCode = l.ProjectCode,
                        ProjectName = l.ProjectName,
                        BonusAmount = l.BonusAmount,
                        Taxable = l.Taxable,
                        InsuranceContributable = l.InsuranceContributable,
                        Reason = l.Reason,
                        Note = l.Note,
                        ValidationStatus = l.ValidationStatus,
                        ErrorMessage = l.ErrorMessage
                    })
                    .ToList()
            };
        }

        private static void ValidateFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Vui lòng chọn file thưởng dự án.");
            var extension = Path.GetExtension(file.FileName);
            if (!string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Hiện tại hệ thống hỗ trợ import file CSV cho thưởng dự án.");
        }

        private static void ValidatePeriod(byte month, short year)
        {
            if (month is < 1 or > 12)
                throw new ArgumentException("Tháng lương không hợp lệ.");
            if (year is < 2000 or > 2100)
                throw new ArgumentException("Năm lương không hợp lệ.");
        }

        private static bool IsEligibleEmployee(Employee employee)
        {
            return employee.Status != EmployeeStatus.Resigned &&
                   employee.Status != EmployeeStatus.Terminated &&
                   employee.Status != EmployeeStatus.Dismissed;
        }

        private static string FormatPeriod(byte month, short year) => $"{month:00}/{year}";

        private static bool TryParsePeriod(string value, out byte month, out short year)
        {
            month = 0;
            year = 0;
            var parts = value.Trim().Split(new[] { '/', '-', '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2) return false;
            if (!int.TryParse(parts[0], out var first) || !int.TryParse(parts[1], out var second)) return false;
            var resolvedMonth = first;
            var resolvedYear = second;
            if (first > 1900)
            {
                resolvedYear = first;
                resolvedMonth = second;
            }

            if (resolvedMonth is < 1 or > 12 || resolvedYear is < 2000 or > 2100) return false;
            month = (byte)resolvedMonth;
            year = (short)resolvedYear;
            return true;
        }

        private static string BuildKey(string employeeCode, string projectCode)
        {
            return $"{NormalizeKey(employeeCode)}|{NormalizeKey(projectCode)}";
        }

        private static string NormalizeKey(string value)
        {
            return (value ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static char ResolveDelimiter(string header)
        {
            var candidates = new[] { ',', ';', '\t' };
            return candidates
                .Select(c => new { Delimiter = c, Count = SplitDelimitedLine(header, c).Count })
                .OrderByDescending(x => x.Count)
                .First().Delimiter;
        }

        private static List<string> SplitDelimitedLine(string line, char delimiter)
        {
            var cells = new List<string>();
            var current = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < line.Length; i++)
            {
                var ch = line[i];
                if (ch == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (ch == delimiter && !inQuotes)
                {
                    cells.Add(current.ToString().Trim());
                    current.Clear();
                }
                else
                {
                    current.Append(ch);
                }
            }

            cells.Add(current.ToString().Trim());
            return cells;
        }

        private static string GetCell(List<string> cells, Dictionary<string, int> headerIndex, int fallbackIndex, params string[] aliases)
        {
            foreach (var alias in aliases)
            {
                if (headerIndex.TryGetValue(NormalizeHeader(alias), out var index) && index >= 0 && index < cells.Count)
                    return cells[index].Trim();
            }

            return fallbackIndex >= 0 && fallbackIndex < cells.Count ? cells[fallbackIndex].Trim() : string.Empty;
        }

        private static string NormalizeHeader(string value)
        {
            var normalized = value.Trim().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder();
            foreach (var ch in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (category == UnicodeCategory.NonSpacingMark) continue;
                if (char.IsLetterOrDigit(ch)) builder.Append(char.ToLowerInvariant(ch));
            }

            return builder.ToString();
        }

        private static bool TryParseMoney(string value, out decimal amount)
        {
            amount = 0;
            if (string.IsNullOrWhiteSpace(value)) return false;

            var cleaned = value
                .Replace("VNĐ", "", StringComparison.OrdinalIgnoreCase)
                .Replace("VND", "", StringComparison.OrdinalIgnoreCase)
                .Replace("đ", "", StringComparison.OrdinalIgnoreCase)
                .Replace(" ", "")
                .Trim();

            var lastComma = cleaned.LastIndexOf(',');
            var lastDot = cleaned.LastIndexOf('.');
            if (lastComma >= 0 && lastDot >= 0)
            {
                if (lastComma > lastDot)
                    cleaned = cleaned.Replace(".", "").Replace(',', '.');
                else
                    cleaned = cleaned.Replace(",", "");
            }
            else if (lastComma >= 0)
            {
                var tail = cleaned.Length - lastComma - 1;
                cleaned = tail == 2 ? cleaned.Replace(',', '.') : cleaned.Replace(",", "");
            }
            else if (lastDot >= 0)
            {
                var tail = cleaned.Length - lastDot - 1;
                if (tail != 2) cleaned = cleaned.Replace(".", "");
            }

            return decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
        }

        private static bool ParseBool(string value, bool defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value)) return defaultValue;
            var normalized = NormalizeHeader(value);
            return normalized switch
            {
                "1" or "true" or "yes" or "y" or "co" or "x" => true,
                "0" or "false" or "no" or "n" or "khong" => false,
                _ => defaultValue
            };
        }

        private static string? EmptyToNull(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string? MergeNote(string? current, string? next)
        {
            if (string.IsNullOrWhiteSpace(next)) return current;
            if (string.IsNullOrWhiteSpace(current)) return next.Trim();
            return $"{current.Trim()}\n{next.Trim()}";
        }

        private static string ResolveStatusText(ProjectBonusImportStatus status)
        {
            return status switch
            {
                ProjectBonusImportStatus.Draft => "Nháp",
                ProjectBonusImportStatus.PendingReview => "Chờ giám đốc duyệt",
                ProjectBonusImportStatus.Approved => "Đã duyệt",
                ProjectBonusImportStatus.Rejected => "Từ chối",
                ProjectBonusImportStatus.Cancelled => "Đã hủy",
                _ => status.ToString()
            };
        }

        private static void EnsureImporter(string role)
        {
            if (IsAnyRole(role, "HR", "Admin", "Director", "Accountant", "Kế toán", "Ke toan")) return;
            throw new UnauthorizedAccessException("Chỉ HR/Kế toán/Admin được import thưởng dự án.");
        }

        private static void EnsureViewer(string role)
        {
            if (IsAnyRole(role, "HR", "Admin", "Director", "Accountant", "Kế toán", "Ke toan")) return;
            throw new UnauthorizedAccessException("Bạn không có quyền xem thưởng dự án.");
        }

        private static void EnsureDirector(string role)
        {
            if (IsAnyRole(role, "Director", "Admin", "Giám đốc", "Giam doc")) return;
            throw new UnauthorizedAccessException("Chỉ Giám đốc/Admin được duyệt thưởng dự án.");
        }

        private static bool IsAnyRole(string role, params string[] accepted)
        {
            return accepted.Any(item => item.Equals(role, StringComparison.OrdinalIgnoreCase));
        }

        private class ParsedProjectBonusRow
        {
            public int RowNumber { get; set; }
            public string EmployeeCode { get; set; } = string.Empty;
            public string PayrollPeriod { get; set; } = string.Empty;
            public string ProjectCode { get; set; } = string.Empty;
            public string ProjectName { get; set; } = string.Empty;
            public decimal BonusAmount { get; set; }
            public bool Taxable { get; set; } = true;
            public bool InsuranceContributable { get; set; }
            public string? Reason { get; set; }
            public string? Note { get; set; }
            public List<string> ParseErrors { get; set; } = new();
        }

        private class ValidatedProjectBonusRow
        {
            public int RowNumber { get; set; }
            public Employee? Employee { get; set; }
            public string EmployeeCode { get; set; } = string.Empty;
            public string? EmployeeName { get; set; }
            public string ProjectCode { get; set; } = string.Empty;
            public string ProjectName { get; set; } = string.Empty;
            public decimal BonusAmount { get; set; }
            public bool Taxable { get; set; }
            public bool InsuranceContributable { get; set; }
            public string? Reason { get; set; }
            public string? Note { get; set; }
            public ProjectBonusLineValidationStatus ValidationStatus { get; set; }
            public string? ErrorMessage { get; set; }
        }

        private class ValidatedProjectBonusImport
        {
            public ProjectBonusImportPreviewDto Preview { get; set; } = new();
            public List<ValidatedProjectBonusRow> ValidRows { get; set; } = new();
        }
    }

    public class ProjectBonusImportValidationException : Exception
    {
        public ProjectBonusImportValidationException(ProjectBonusImportPreviewDto preview)
            : base("File thưởng dự án có dữ liệu không hợp lệ.")
        {
            Preview = preview;
        }

        public ProjectBonusImportPreviewDto Preview { get; }
    }
}

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
    public class ExternalTimesheetImportUseCase : IExternalTimesheetImportUseCase
    {
        private const string DefaultSourceSystem = "Timesheet cộng tác viên";

        private readonly IExternalTimesheetImportRepository _timesheetRepo;
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IPayrollRepository _payrollRepo;
        private readonly IAuditLogRepository _auditRepo;
        private readonly ILockService _lockService;
        private readonly IUnitOfWork _unitOfWork;

        public ExternalTimesheetImportUseCase(
            IExternalTimesheetImportRepository timesheetRepo,
            IEmployeeRepository employeeRepo,
            IPayrollRepository payrollRepo,
            IAuditLogRepository auditRepo,
            ILockService lockService,
            IUnitOfWork unitOfWork)
        {
            _timesheetRepo = timesheetRepo;
            _employeeRepo = employeeRepo;
            _payrollRepo = payrollRepo;
            _auditRepo = auditRepo;
            _lockService = lockService;
            _unitOfWork = unitOfWork;
        }

        public async Task<ExternalTimesheetImportPreviewDto> PreviewAsync(ExternalTimesheetImportRequestDto dto, int actorAccountId, string actorRole, CancellationToken ct = default)
        {
            EnsureImporter(actorRole);
            var result = await BuildValidatedImportAsync(dto, ct);
            return result.Preview;
        }

        public async Task<ExternalTimesheetImportBatchDto> ImportAsync(ExternalTimesheetImportRequestDto dto, int actorAccountId, string actorRole, CancellationToken ct = default)
        {
            EnsureImporter(actorRole);
            ValidatePeriod(dto.ImportMonth, dto.ImportYear);

            return await _lockService.GetWithLockAsync(
                LockKeys.ExternalTimesheetPeriod(dto.ImportMonth, dto.ImportYear),
                innerCt => ImportCoreAsync(dto, actorAccountId, actorRole, innerCt),
                TimeSpan.FromSeconds(30),
                ct);
        }

        private async Task<ExternalTimesheetImportBatchDto> ImportCoreAsync(ExternalTimesheetImportRequestDto dto, int actorAccountId, string actorRole, CancellationToken ct = default)
        {
            EnsureImporter(actorRole);
            var result = await BuildValidatedImportAsync(dto, ct);
            if (!result.Preview.CanSave)
                throw new ExternalTimesheetImportValidationException(result.Preview);

            var import = new ExternalTimesheetImport
            {
                SourceSystem = ResolveSourceSystem(dto.SourceSystem),
                ImportMonth = dto.ImportMonth,
                ImportYear = dto.ImportYear,
                PayrollPeriod = FormatPeriod(dto.ImportMonth, dto.ImportYear),
                FileName = dto.File.FileName,
                ImportedByAccountId = actorAccountId,
                ImportedAt = DateTime.UtcNow,
                Status = ExternalTimesheetImportStatus.Draft,
                TotalRows = result.ValidRows.Count,
                ValidRows = result.ValidRows.Count,
                ErrorRows = 0,
                TotalHours = result.ValidRows.Sum(r => r.ApprovedHours),
                TotalAmount = result.ValidRows.Sum(r => r.Amount),
                Note = dto.Note
            };

            foreach (var row in result.ValidRows)
            {
                import.Lines.Add(new ExternalTimesheetLine
                {
                    RowNumber = row.RowNumber,
                    CollaboratorEmployeeId = row.Employee!.Id,
                    CollaboratorCode = row.Employee.EmployeeCode,
                    CollaboratorNameSnapshot = row.Employee.FullName,
                    WorkDate = row.WorkDate!.Value.Date,
                    ProjectCode = row.ProjectCode,
                    TaskCode = row.TaskCode,
                    ApprovedHours = row.ApprovedHours,
                    HourlyRate = row.HourlyRate,
                    Amount = row.Amount,
                    ValidationStatus = ProjectBonusLineValidationStatus.Valid,
                    Note = row.Note
                });
            }

            await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                if (dto.Overwrite)
                    await RemoveDuplicateLinesForOverwriteAsync(result.ValidRows, dto.ImportMonth, dto.ImportYear, actorAccountId, ct);

                await _timesheetRepo.AddAsync(import, ct);
                await _auditRepo.LogSystemEventAsync(
                    "EXTERNAL_TIMESHEET_IMPORT_CREATED",
                    actorAccountId,
                    "external_timesheet_imports",
                    $"Imported external timesheet draft for {import.PayrollPeriod}. Rows={import.ValidRows}, Hours={import.TotalHours}, Amount={import.TotalAmount}.");
                await _unitOfWork.CommitAsync(ct);
            }, ct);

            return MapBatch(import);
        }

        public async Task<List<ExternalTimesheetImportBatchDto>> GetBatchesAsync(byte? month, short? year, ExternalTimesheetImportStatus? status, string actorRole, CancellationToken ct = default)
        {
            EnsureViewer(actorRole);
            var batches = await _timesheetRepo.GetBatchesAsync(month, year, status, ct);
            return batches.Select(MapBatch).ToList();
        }

        public async Task<ExternalTimesheetImportBatchDto> GetDetailAsync(int id, string actorRole, CancellationToken ct = default)
        {
            EnsureViewer(actorRole);
            var batch = await _timesheetRepo.GetDetailAsync(id, ct)
                ?? throw new KeyNotFoundException("Không tìm thấy batch giờ công cộng tác viên.");
            return MapBatch(batch);
        }

        public async Task<ExternalTimesheetImportBatchDto> SubmitAsync(int id, int actorAccountId, string actorRole, CancellationToken ct = default)
        {
            EnsureImporter(actorRole);
            return await _lockService.GetWithLockAsync(
                LockKeys.ExternalTimesheetBatch(id),
                innerCt => SubmitCoreAsync(id, actorAccountId, actorRole, innerCt),
                TimeSpan.FromSeconds(20),
                ct);
        }

        private async Task<ExternalTimesheetImportBatchDto> SubmitCoreAsync(int id, int actorAccountId, string actorRole, CancellationToken ct = default)
        {
            EnsureImporter(actorRole);
            var batch = await _timesheetRepo.GetTrackedDetailAsync(id, ct)
                ?? throw new KeyNotFoundException("Không tìm thấy batch giờ công cộng tác viên.");

            if (batch.Status != ExternalTimesheetImportStatus.Draft)
                throw new InvalidOperationException("Chỉ batch ở trạng thái nháp mới được gửi duyệt.");
            if (batch.ErrorRows > 0 || batch.Lines.Any(l => l.ValidationStatus != ProjectBonusLineValidationStatus.Valid))
                throw new InvalidOperationException("Batch còn dòng lỗi, không thể gửi duyệt.");
            if (await _payrollRepo.HasLockedPayrollAsync(batch.ImportMonth, batch.ImportYear, ct))
                throw new InvalidOperationException("Kỳ lương đã khóa/chốt, không thể gửi duyệt giờ công cộng tác viên.");

            batch.Status = ExternalTimesheetImportStatus.Validated;
            _timesheetRepo.Update(batch);
            await _auditRepo.LogSystemEventAsync(
                "EXTERNAL_TIMESHEET_IMPORT_SUBMITTED",
                actorAccountId,
                "external_timesheet_imports",
                $"Submitted external timesheet import #{batch.Id} for director review.");
            await _unitOfWork.CommitAsync(ct);
            return MapBatch(batch);
        }

        public async Task<ExternalTimesheetImportBatchDto> DirectorReviewAsync(int id, ReviewExternalTimesheetImportDto dto, int actorAccountId, string actorRole, CancellationToken ct = default)
        {
            EnsureDirector(actorRole);
            return await _lockService.GetWithLockAsync(
                LockKeys.ExternalTimesheetBatch(id),
                innerCt => DirectorReviewCoreAsync(id, dto, actorAccountId, actorRole, innerCt),
                TimeSpan.FromSeconds(20),
                ct);
        }

        private async Task<ExternalTimesheetImportBatchDto> DirectorReviewCoreAsync(int id, ReviewExternalTimesheetImportDto dto, int actorAccountId, string actorRole, CancellationToken ct = default)
        {
            EnsureDirector(actorRole);
            var batch = await _timesheetRepo.GetTrackedDetailAsync(id, ct)
                ?? throw new KeyNotFoundException("Không tìm thấy batch giờ công cộng tác viên.");

            if (batch.Status != ExternalTimesheetImportStatus.Validated)
                throw new InvalidOperationException("Chỉ batch đang chờ duyệt mới được xử lý.");
            if (await _payrollRepo.HasLockedPayrollAsync(batch.ImportMonth, batch.ImportYear, ct))
                throw new InvalidOperationException("Kỳ lương đã khóa/chốt, không thể duyệt giờ công cộng tác viên.");

            batch.Status = dto.IsApproved ? ExternalTimesheetImportStatus.Approved : ExternalTimesheetImportStatus.Rejected;
            batch.ApprovedByAccountId = actorAccountId;
            batch.ApprovedAt = DateTime.UtcNow;
            batch.Note = MergeNote(batch.Note, dto.Note);
            _timesheetRepo.Update(batch);

            await _auditRepo.LogSystemEventAsync(
                dto.IsApproved ? "EXTERNAL_TIMESHEET_IMPORT_APPROVED" : "EXTERNAL_TIMESHEET_IMPORT_REJECTED",
                actorAccountId,
                "external_timesheet_imports",
                $"Director reviewed external timesheet import #{batch.Id}. Approved={dto.IsApproved}.");
            await _unitOfWork.CommitAsync(ct);
            return MapBatch(batch);
        }

        public async Task<ExternalTimesheetImportBatchDto> CancelAsync(int id, int actorAccountId, string actorRole, string? note, CancellationToken ct = default)
        {
            EnsureImporter(actorRole);
            return await _lockService.GetWithLockAsync(
                LockKeys.ExternalTimesheetBatch(id),
                innerCt => CancelCoreAsync(id, actorAccountId, actorRole, note, innerCt),
                TimeSpan.FromSeconds(20),
                ct);
        }

        private async Task<ExternalTimesheetImportBatchDto> CancelCoreAsync(int id, int actorAccountId, string actorRole, string? note, CancellationToken ct = default)
        {
            EnsureImporter(actorRole);
            var batch = await _timesheetRepo.GetTrackedDetailAsync(id, ct)
                ?? throw new KeyNotFoundException("Không tìm thấy batch giờ công cộng tác viên.");

            if (batch.Status is ExternalTimesheetImportStatus.Approved or ExternalTimesheetImportStatus.PayrollImported)
                throw new InvalidOperationException("Batch đã duyệt hoặc đã đưa vào payroll không được hủy. Hãy tạo batch điều chỉnh nếu cần thay đổi.");
            if (batch.Status == ExternalTimesheetImportStatus.Cancelled)
                return MapBatch(batch);

            batch.Status = ExternalTimesheetImportStatus.Cancelled;
            batch.Note = MergeNote(batch.Note, note);
            _timesheetRepo.Update(batch);

            await _auditRepo.LogSystemEventAsync(
                "EXTERNAL_TIMESHEET_IMPORT_CANCELLED",
                actorAccountId,
                "external_timesheet_imports",
                $"Cancelled external timesheet import #{batch.Id} for {batch.PayrollPeriod}.");
            await _unitOfWork.CommitAsync(ct);
            return MapBatch(batch);
        }

        public async Task<List<ExternalTimesheetImportBatchDto>> GetPendingDirectorAsync(int actorAccountId, string actorRole, CancellationToken ct = default)
        {
            EnsureDirector(actorRole);
            var batches = await _timesheetRepo.GetBatchesAsync(null, null, ExternalTimesheetImportStatus.Validated, ct);
            return batches.Select(MapBatch).ToList();
        }

        private async Task<ValidatedExternalTimesheetImport> BuildValidatedImportAsync(ExternalTimesheetImportRequestDto dto, CancellationToken ct)
        {
            ValidatePeriod(dto.ImportMonth, dto.ImportYear);
            ValidateFile(dto.File);

            var periodStart = new DateTime(dto.ImportYear, dto.ImportMonth, 1);
            var periodEnd = periodStart.AddMonths(1).AddTicks(-1);
            var parsedRows = await ParseCsvAsync(dto.File, ct);
            var globalErrors = new List<string>();
            if (await _payrollRepo.HasLockedPayrollAsync(dto.ImportMonth, dto.ImportYear, ct))
                globalErrors.Add("Kỳ lương đã khóa/chốt, không thể import giờ công cộng tác viên.");

            var employeeCodes = parsedRows
                .Where(r => !string.IsNullOrWhiteSpace(r.CollaboratorCode))
                .Select(r => r.CollaboratorCode.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var employees = employeeCodes.Count == 0
                ? new List<Employee>()
                : (await _employeeRepo.FindAsync(e => employeeCodes.Contains(e.EmployeeCode), ct)).ToList();
            var employeeByCode = employees
                .GroupBy(e => e.EmployeeCode.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var existingCandidates = await _timesheetRepo.GetDuplicateCandidatesAsync(periodStart, periodEnd, ct);
            var existingKeys = existingCandidates
                .Select(BuildKey)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var approvedKeys = existingCandidates
                .Where(l => l.Import.Status is ExternalTimesheetImportStatus.Approved or ExternalTimesheetImportStatus.PayrollImported)
                .Select(BuildKey)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var duplicateRowsInFile = parsedRows
                .Where(r => !string.IsNullOrWhiteSpace(r.CollaboratorCode) &&
                            r.WorkDate.HasValue &&
                            !string.IsNullOrWhiteSpace(r.ProjectCode))
                .GroupBy(BuildKey, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var validatedRows = new List<ValidatedExternalTimesheetRow>();
            var lineDtos = new List<ExternalTimesheetImportLineDto>();

            foreach (var row in parsedRows)
            {
                var errors = new List<string>(row.ParseErrors);
                Employee? employee = null;

                if (string.IsNullOrWhiteSpace(row.CollaboratorCode))
                {
                    errors.Add("Thiếu mã nhân sự/cộng tác viên.");
                }
                else if (!employeeByCode.TryGetValue(row.CollaboratorCode.Trim(), out employee))
                {
                    errors.Add($"Mã nhân sự {row.CollaboratorCode} không tồn tại.");
                }
                else if (!IsEligibleEmployee(employee))
                {
                    errors.Add($"Nhân sự {employee.EmployeeCode} đang ở trạng thái {employee.Status}, không hợp lệ để tính giờ công kỳ này.");
                }

                if (!string.IsNullOrWhiteSpace(row.PayrollPeriod))
                {
                    if (!TryParsePeriod(row.PayrollPeriod, out var rowMonth, out var rowYear))
                        errors.Add("Kỳ lương trong file không hợp lệ. Định dạng gợi ý: MM/yyyy.");
                    else if (rowMonth != dto.ImportMonth || rowYear != dto.ImportYear)
                        errors.Add($"Kỳ lương trong dòng ({rowMonth:00}/{rowYear}) không khớp kỳ đang import ({dto.ImportMonth:00}/{dto.ImportYear}).");
                }

                if (!row.WorkDate.HasValue)
                {
                    errors.Add("Ngày công không hợp lệ.");
                }
                else if (row.WorkDate.Value.Date < periodStart.Date || row.WorkDate.Value.Date > periodEnd.Date)
                {
                    errors.Add($"Ngày công {row.WorkDate.Value:dd/MM/yyyy} không thuộc kỳ lương {dto.ImportMonth:00}/{dto.ImportYear}.");
                }

                if (string.IsNullOrWhiteSpace(row.ProjectCode))
                    errors.Add("Thiếu mã dự án.");
                if (row.ApprovedHours <= 0)
                    errors.Add("Số giờ duyệt phải lớn hơn 0.");
                if (row.ApprovedHours > 24)
                    errors.Add("Số giờ duyệt trong một dòng không được vượt quá 24.");
                if (row.HourlyRate < 0)
                    errors.Add("Đơn giá không được âm.");
                if (row.Amount <= 0)
                    errors.Add("Thành tiền phải lớn hơn 0.");

                var key = BuildKey(row);
                if (duplicateRowsInFile.Contains(key))
                    errors.Add("Dòng giờ công bị trùng trong chính file theo Mã nhân sự + Ngày công + Mã dự án + Task.");
                if (!dto.Overwrite && existingKeys.Contains(key))
                    errors.Add("Dòng giờ công đã tồn tại trong hệ thống cho kỳ này. Bật chế độ ghi đè nếu cần thay thế dòng chưa duyệt.");
                if (dto.Overwrite && approvedKeys.Contains(key))
                    errors.Add("Dòng giờ công đã thuộc batch được duyệt hoặc đã đưa vào payroll, không thể ghi đè.");

                var validationStatus = errors.Count == 0
                    ? ProjectBonusLineValidationStatus.Valid
                    : ProjectBonusLineValidationStatus.Invalid;
                var validated = new ValidatedExternalTimesheetRow
                {
                    RowNumber = row.RowNumber,
                    Employee = employee,
                    CollaboratorCode = row.CollaboratorCode.Trim(),
                    CollaboratorName = employee?.FullName ?? EmptyToNull(row.CollaboratorName),
                    PayrollPeriod = row.PayrollPeriod,
                    WorkDate = row.WorkDate,
                    ProjectCode = row.ProjectCode.Trim(),
                    TaskCode = EmptyToNull(row.TaskCode) ?? string.Empty,
                    ApprovedHours = row.ApprovedHours,
                    HourlyRate = row.HourlyRate,
                    Amount = row.Amount,
                    Note = row.Note,
                    ValidationStatus = validationStatus,
                    ErrorMessage = errors.Count == 0 ? null : string.Join("; ", errors)
                };
                validatedRows.Add(validated);
                lineDtos.Add(MapValidatedRow(validated));
            }

            if (parsedRows.Count == 0)
                globalErrors.Add("File không có dòng giờ công hợp lệ.");

            var validRows = validatedRows
                .Where(r => r.ValidationStatus == ProjectBonusLineValidationStatus.Valid)
                .ToList();
            var preview = new ExternalTimesheetImportPreviewDto
            {
                ImportMonth = dto.ImportMonth,
                ImportYear = dto.ImportYear,
                PayrollPeriod = FormatPeriod(dto.ImportMonth, dto.ImportYear),
                SourceSystem = ResolveSourceSystem(dto.SourceSystem),
                FileName = dto.File.FileName,
                Overwrite = dto.Overwrite,
                TotalRows = parsedRows.Count,
                ValidRows = validRows.Count,
                ErrorRows = validatedRows.Count(r => r.ValidationStatus == ProjectBonusLineValidationStatus.Invalid) + globalErrors.Count,
                TotalHours = validRows.Sum(r => r.ApprovedHours),
                TotalAmount = validRows.Sum(r => r.Amount),
                GlobalErrors = globalErrors,
                Lines = lineDtos
            };
            preview.CanSave = preview.ErrorRows == 0 && preview.TotalRows > 0;

            return new ValidatedExternalTimesheetImport
            {
                Preview = preview,
                ValidRows = validRows
            };
        }

        private async Task RemoveDuplicateLinesForOverwriteAsync(List<ValidatedExternalTimesheetRow> rows, byte month, short year, int actorAccountId, CancellationToken ct)
        {
            var periodStart = new DateTime(year, month, 1);
            var periodEnd = periodStart.AddMonths(1).AddTicks(-1);
            var keys = rows.Select(BuildKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var candidates = await _timesheetRepo.GetReplaceableDuplicateCandidatesAsync(periodStart, periodEnd, ct);
            var duplicates = candidates
                .Where(l => keys.Contains(BuildKey(l)))
                .ToList();

            if (duplicates.Count == 0) return;

            foreach (var group in duplicates.GroupBy(l => l.Import))
            {
                var import = group.Key;
                var removed = group.ToList();
                import.TotalRows = Math.Max(0, import.TotalRows - removed.Count);
                import.ValidRows = Math.Max(0, import.ValidRows - removed.Count(l => l.ValidationStatus == ProjectBonusLineValidationStatus.Valid));
                import.ErrorRows = Math.Max(0, import.ErrorRows - removed.Count(l => l.ValidationStatus == ProjectBonusLineValidationStatus.Invalid));
                import.TotalHours = Math.Max(0, import.TotalHours - removed.Where(l => l.ValidationStatus == ProjectBonusLineValidationStatus.Valid).Sum(l => l.ApprovedHours));
                import.TotalAmount = Math.Max(0, import.TotalAmount - removed.Where(l => l.ValidationStatus == ProjectBonusLineValidationStatus.Valid).Sum(l => l.Amount));
                if (import.TotalRows == 0)
                    import.Status = ExternalTimesheetImportStatus.Cancelled;
                _timesheetRepo.Update(import);
            }

            _timesheetRepo.RemoveLines(duplicates);
            await _auditRepo.LogSystemEventAsync(
                "EXTERNAL_TIMESHEET_IMPORT_OVERWRITE",
                actorAccountId,
                "external_timesheet_lines",
                $"Removed {duplicates.Count} duplicated external timesheet lines before overwrite for {month:00}/{year}.");
        }

        private static async Task<List<ParsedExternalTimesheetRow>> ParseCsvAsync(IFormFile file, CancellationToken ct)
        {
            await using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var content = await reader.ReadToEndAsync(ct);
            var rawRows = content
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();
            if (rawRows.Count <= 1) return new List<ParsedExternalTimesheetRow>();

            var delimiter = ResolveDelimiter(rawRows[0]);
            var headers = SplitDelimitedLine(rawRows[0], delimiter);
            var headerIndex = headers
                .Select((header, index) => new { Key = NormalizeHeader(header), Index = index })
                .Where(x => !string.IsNullOrWhiteSpace(x.Key))
                .GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Index, StringComparer.OrdinalIgnoreCase);

            var rows = new List<ParsedExternalTimesheetRow>();
            for (var i = 1; i < rawRows.Count; i++)
            {
                var cells = SplitDelimitedLine(rawRows[i], delimiter);
                var errors = new List<string>();
                var workDateText = GetCell(cells, headerIndex, 2, "NgayLam", "Ngày công", "WorkDate", "Date");
                if (!TryParseDate(workDateText, out var workDate))
                    errors.Add("Ngày công không hợp lệ.");

                var hoursText = GetCell(cells, headerIndex, 5, "SoGioDuyet", "Số giờ duyệt", "ApprovedHours", "Hours");
                if (!TryParseNumber(hoursText, out var approvedHours))
                    errors.Add("Số giờ duyệt không hợp lệ.");

                var rateText = GetCell(cells, headerIndex, 6, "DonGia", "Đơn giá", "HourlyRate", "Rate");
                if (!TryParseMoney(rateText, out var hourlyRate))
                    errors.Add("Đơn giá không hợp lệ.");

                rows.Add(new ParsedExternalTimesheetRow
                {
                    RowNumber = i + 1,
                    CollaboratorCode = GetCell(cells, headerIndex, 0, "MaNhanVien", "Mã nhân viên", "MaCongTacVien", "Mã cộng tác viên", "CollaboratorCode", "EmployeeCode"),
                    CollaboratorName = GetCell(cells, headerIndex, 1, "HoTen", "Họ tên", "TenCongTacVien", "Tên cộng tác viên", "CollaboratorName", "EmployeeName"),
                    WorkDate = workDate,
                    WorkDateText = workDateText,
                    ProjectCode = GetCell(cells, headerIndex, 3, "MaDuAn", "Mã dự án", "ProjectCode"),
                    TaskCode = GetCell(cells, headerIndex, 4, "MaCongViec", "Mã công việc", "TaskCode", "Task"),
                    ApprovedHours = approvedHours,
                    HourlyRate = hourlyRate,
                    Amount = approvedHours * hourlyRate,
                    PayrollPeriod = GetCell(cells, headerIndex, -1, "KyLuong", "Kỳ lương", "PayrollPeriod", "Period"),
                    Note = EmptyToNull(GetCell(cells, headerIndex, 7, "GhiChu", "Ghi chú", "Note")),
                    ParseErrors = errors
                });
            }

            return rows;
        }

        private static ExternalTimesheetImportLineDto MapValidatedRow(ValidatedExternalTimesheetRow row)
        {
            return new ExternalTimesheetImportLineDto
            {
                RowNumber = row.RowNumber,
                CollaboratorEmployeeId = row.Employee?.Id,
                CollaboratorCode = row.CollaboratorCode,
                CollaboratorName = row.CollaboratorName,
                WorkDate = row.WorkDate,
                WorkDateText = row.WorkDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                ProjectCode = row.ProjectCode,
                TaskCode = row.TaskCode,
                ApprovedHours = row.ApprovedHours,
                HourlyRate = row.HourlyRate,
                Amount = row.Amount,
                Note = row.Note,
                ValidationStatus = row.ValidationStatus,
                ErrorMessage = row.ErrorMessage
            };
        }

        private static ExternalTimesheetImportBatchDto MapBatch(ExternalTimesheetImport batch)
        {
            return new ExternalTimesheetImportBatchDto
            {
                Id = batch.Id,
                ImportMonth = batch.ImportMonth,
                ImportYear = batch.ImportYear,
                PayrollPeriod = batch.PayrollPeriod,
                SourceSystem = batch.SourceSystem,
                FileName = batch.FileName ?? batch.FileUrl ?? string.Empty,
                Status = batch.Status,
                StatusText = ResolveStatusText(batch.Status),
                TotalRows = batch.TotalRows == 0 && batch.Lines.Count > 0 ? batch.Lines.Count : batch.TotalRows,
                ValidRows = batch.ValidRows == 0 && batch.Lines.Count > 0 ? batch.Lines.Count : batch.ValidRows,
                ErrorRows = batch.ErrorRows,
                TotalHours = batch.TotalHours == 0 && batch.Lines.Count > 0 ? batch.Lines.Sum(l => l.ApprovedHours) : batch.TotalHours,
                TotalAmount = batch.TotalAmount == 0 && batch.Lines.Count > 0 ? batch.Lines.Sum(l => l.Amount) : batch.TotalAmount,
                ImportedByAccountId = batch.ImportedByAccountId,
                ImportedByName = batch.ImportedByAccount?.FullName ?? batch.ImportedByAccount?.Email,
                ImportedAt = batch.ImportedAt,
                ApprovedByAccountId = batch.ApprovedByAccountId,
                ApprovedByName = batch.ApprovedByAccount?.FullName ?? batch.ApprovedByAccount?.Email,
                ApprovedAt = batch.ApprovedAt,
                Note = batch.Note,
                Lines = batch.Lines
                    .OrderBy(l => l.RowNumber)
                    .Select(l => new ExternalTimesheetImportLineDto
                    {
                        Id = l.Id,
                        RowNumber = l.RowNumber,
                        CollaboratorEmployeeId = l.CollaboratorEmployeeId,
                        CollaboratorCode = l.CollaboratorCode ?? l.CollaboratorEmployee?.EmployeeCode ?? string.Empty,
                        CollaboratorName = l.CollaboratorNameSnapshot ?? l.CollaboratorEmployee?.FullName,
                        WorkDate = l.WorkDate,
                        WorkDateText = l.WorkDate.ToString("yyyy-MM-dd"),
                        ProjectCode = l.ProjectCode ?? string.Empty,
                        TaskCode = l.TaskCode ?? string.Empty,
                        ApprovedHours = l.ApprovedHours,
                        HourlyRate = l.HourlyRate,
                        Amount = l.Amount,
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
                throw new ArgumentException("Vui lòng chọn file giờ công cộng tác viên.");
            var extension = Path.GetExtension(file.FileName);
            if (!string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Hiện tại hệ thống hỗ trợ import file CSV cho giờ công cộng tác viên.");
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

        private static bool TryParseDate(string value, out DateTime? date)
        {
            date = null;
            if (string.IsNullOrWhiteSpace(value)) return false;
            var formats = new[]
            {
                "yyyy-MM-dd",
                "dd/MM/yyyy",
                "d/M/yyyy",
                "MM/dd/yyyy",
                "M/d/yyyy",
                "dd-MM-yyyy",
                "d-M-yyyy",
                "yyyy/MM/dd"
            };
            if (DateTime.TryParseExact(value.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact) ||
                DateTime.TryParse(value.Trim(), new CultureInfo("vi-VN"), DateTimeStyles.None, out exact) ||
                DateTime.TryParse(value.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out exact))
            {
                date = exact.Date;
                return true;
            }

            return false;
        }

        private static string BuildKey(ExternalTimesheetLine line)
        {
            return $"{NormalizeKey(line.CollaboratorCode ?? line.CollaboratorEmployee?.EmployeeCode ?? string.Empty)}|{line.WorkDate:yyyyMMdd}|{NormalizeKey(line.ProjectCode ?? string.Empty)}|{NormalizeKey(line.TaskCode ?? string.Empty)}";
        }

        private static string BuildKey(ValidatedExternalTimesheetRow row)
        {
            return $"{NormalizeKey(row.CollaboratorCode)}|{row.WorkDate?.ToString("yyyyMMdd") ?? string.Empty}|{NormalizeKey(row.ProjectCode)}|{NormalizeKey(row.TaskCode)}";
        }

        private static string BuildKey(ParsedExternalTimesheetRow row)
        {
            return $"{NormalizeKey(row.CollaboratorCode)}|{row.WorkDate?.ToString("yyyyMMdd") ?? string.Empty}|{NormalizeKey(row.ProjectCode)}|{NormalizeKey(row.TaskCode)}";
        }

        private static string NormalizeKey(string value)
        {
            return (value ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static string ResolveSourceSystem(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? DefaultSourceSystem : value.Trim();
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

        private static bool TryParseNumber(string value, out decimal amount)
        {
            return TryParseMoney(value, out amount);
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

        private static string ResolveStatusText(ExternalTimesheetImportStatus status)
        {
            return status switch
            {
                ExternalTimesheetImportStatus.Draft => "Nháp",
                ExternalTimesheetImportStatus.Imported => "Đã import",
                ExternalTimesheetImportStatus.Validated => "Chờ giám đốc duyệt",
                ExternalTimesheetImportStatus.Approved => "Đã duyệt",
                ExternalTimesheetImportStatus.Rejected => "Từ chối",
                ExternalTimesheetImportStatus.PayrollImported => "Đã đưa vào lương",
                ExternalTimesheetImportStatus.Cancelled => "Đã hủy",
                _ => status.ToString()
            };
        }

        private static void EnsureImporter(string role)
        {
            if (IsAnyRole(role, "HR", "Admin", "Accountant", "Kế toán", "Ke toan")) return;
            throw new UnauthorizedAccessException("Chỉ HR/Kế toán/Admin được import giờ công cộng tác viên.");
        }

        private static void EnsureViewer(string role)
        {
            if (IsAnyRole(role, "HR", "Admin", "Director", "Accountant", "Kế toán", "Ke toan")) return;
            throw new UnauthorizedAccessException("Bạn không có quyền xem giờ công cộng tác viên.");
        }

        private static void EnsureDirector(string role)
        {
            if (IsAnyRole(role, "Director", "Admin", "Giám đốc", "Giam doc")) return;
            throw new UnauthorizedAccessException("Chỉ Giám đốc/Admin được duyệt giờ công cộng tác viên.");
        }

        private static bool IsAnyRole(string role, params string[] accepted)
        {
            return accepted.Any(item => item.Equals(role, StringComparison.OrdinalIgnoreCase));
        }

        private class ParsedExternalTimesheetRow
        {
            public int RowNumber { get; set; }
            public string CollaboratorCode { get; set; } = string.Empty;
            public string CollaboratorName { get; set; } = string.Empty;
            public string PayrollPeriod { get; set; } = string.Empty;
            public DateTime? WorkDate { get; set; }
            public string WorkDateText { get; set; } = string.Empty;
            public string ProjectCode { get; set; } = string.Empty;
            public string TaskCode { get; set; } = string.Empty;
            public decimal ApprovedHours { get; set; }
            public decimal HourlyRate { get; set; }
            public decimal Amount { get; set; }
            public string? Note { get; set; }
            public List<string> ParseErrors { get; set; } = new();
        }

        private class ValidatedExternalTimesheetRow
        {
            public int RowNumber { get; set; }
            public Employee? Employee { get; set; }
            public string CollaboratorCode { get; set; } = string.Empty;
            public string? CollaboratorName { get; set; }
            public string PayrollPeriod { get; set; } = string.Empty;
            public DateTime? WorkDate { get; set; }
            public string ProjectCode { get; set; } = string.Empty;
            public string TaskCode { get; set; } = string.Empty;
            public decimal ApprovedHours { get; set; }
            public decimal HourlyRate { get; set; }
            public decimal Amount { get; set; }
            public string? Note { get; set; }
            public ProjectBonusLineValidationStatus ValidationStatus { get; set; }
            public string? ErrorMessage { get; set; }
        }

        private class ValidatedExternalTimesheetImport
        {
            public ExternalTimesheetImportPreviewDto Preview { get; set; } = new();
            public List<ValidatedExternalTimesheetRow> ValidRows { get; set; } = new();
        }
    }

    public class ExternalTimesheetImportValidationException : Exception
    {
        public ExternalTimesheetImportValidationException(ExternalTimesheetImportPreviewDto preview)
            : base("File giờ công cộng tác viên có dữ liệu không hợp lệ.")
        {
            Preview = preview;
        }

        public ExternalTimesheetImportPreviewDto Preview { get; }
    }
}

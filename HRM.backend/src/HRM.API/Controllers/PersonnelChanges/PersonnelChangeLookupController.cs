using System.Security.Claims;
using HRM.backend.src.HRM.API.Extensions;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.API.Controllers.PersonnelChanges
{
    [ApiController]
    [Authorize]
    [Route("api/v1/personnel-changes/lookups")]
    public class PersonnelChangeLookupController : ControllerBase
    {
        private const long MaxEvidenceFileSize = 10 * 1024 * 1024;
        private static readonly HashSet<string> AllowedEvidenceExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png", ".webp"
        };

        private readonly MyDbContext _db;
        private readonly IStorageService _storageService;

        public PersonnelChangeLookupController(MyDbContext db, IStorageService storageService)
        {
            _db = db;
            _storageService = storageService;
        }

        [HttpGet("employees")]
        public async Task<IActionResult> GetEmployees([FromQuery] string? search, CancellationToken ct)
        {
            var query = ApplyEmployeeScope(BaseEmployeeQuery(), await GetActorEmployeeAsync(ct), GetRole())
                .Where(e => e.Status != EmployeeStatus.Resigned &&
                            e.Status != EmployeeStatus.Terminated &&
                            e.Status != EmployeeStatus.Dismissed);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();
                query = query.Where(e =>
                    e.FullName.Contains(keyword) ||
                    e.EmployeeCode.Contains(keyword) ||
                    (e.Department != null && e.Department.DeptName.Contains(keyword)) ||
                    (e.Position != null && e.Position.Title.Contains(keyword)));
            }

            var employees = await query
                .OrderBy(e => e.FullName)
                .Take(100)
                .ToListAsync(ct);
            var data = employees.Select(MapEmployeeOption).ToList();

            return Ok(new { Success = true, Data = data });
        }

        [HttpGet("departments")]
        public async Task<IActionResult> GetDepartments(CancellationToken ct)
        {
            var data = await _db.Departments
                .AsNoTracking()
                .Where(d => d.Status == DeptStatus.Active)
                .OrderBy(d => d.DeptName)
                .Select(d => new PersonnelChangeDepartmentOptionDto
                {
                    Id = d.Id,
                    DeptCode = d.DeptCode,
                    DeptName = d.DeptName,
                    ParentDeptId = d.ParentDeptId,
                    ManagerId = d.ManagerId,
                    ManagerName = d.Manager != null ? d.Manager.FullName : null
                })
                .ToListAsync(ct);

            return Ok(new { Success = true, Data = data });
        }

        [HttpGet("positions")]
        public async Task<IActionResult> GetPositions(CancellationToken ct)
        {
            var data = await _db.Positions
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.Title)
                .Select(p => new PersonnelChangePositionOptionDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    JobLevel = p.JobLevel
                })
                .ToListAsync(ct);

            return Ok(new { Success = true, Data = data });
        }

        [HttpGet("job-levels")]
        public async Task<IActionResult> GetJobLevels(CancellationToken ct)
        {
            var data = await _db.JobLevels
                .AsNoTracking()
                .Where(j => j.IsActive)
                .OrderBy(j => j.RankOrder)
                .ThenBy(j => j.Name)
                .Select(j => new PersonnelChangeJobLevelOptionDto
                {
                    Id = j.Id,
                    Code = j.Code,
                    Name = j.Name,
                    RankOrder = j.RankOrder,
                    IsManagementLevel = j.IsManagementLevel
                })
                .ToListAsync(ct);

            return Ok(new { Success = true, Data = data });
        }

        [HttpGet("managers")]
        public async Task<IActionResult> GetManagers([FromQuery] string? search, CancellationToken ct)
        {
            var query = ApplyEmployeeScope(BaseEmployeeQuery(), await GetActorEmployeeAsync(ct), GetRole())
                .Where(e => e.Status != EmployeeStatus.Resigned &&
                            e.Status != EmployeeStatus.Terminated &&
                            e.Status != EmployeeStatus.Dismissed);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();
                query = query.Where(e =>
                    e.FullName.Contains(keyword) ||
                    e.EmployeeCode.Contains(keyword) ||
                    (e.Department != null && e.Department.DeptName.Contains(keyword)));
            }

            var managers = await query
                .OrderBy(e => e.FullName)
                .Take(100)
                .ToListAsync(ct);
            var data = managers.Select(MapEmployeeOption).ToList();

            return Ok(new { Success = true, Data = data });
        }

        [HttpGet("employees/{employeeId:int}/penalties")]
        public async Task<IActionResult> GetEmployeePenalties(int employeeId, CancellationToken ct)
        {
            await EnsureCanAccessEmployeeAsync(employeeId, ct);

            var records = await _db.PenaltyRecords
                .AsNoTracking()
                .Where(p => p.EmployeeId == employeeId)
                .OrderByDescending(p => p.OccurredAt ?? p.CreatedAt)
                .Take(50)
                .ToListAsync(ct);

            var data = records.Select(p => new PersonnelChangePenaltyOptionDto
                {
                    Id = p.Id,
                    Period = p.Period,
                    RuleCode = p.RuleCode,
                    Reason = p.Reason,
                    PenaltyPoint = p.PenaltyPoint,
                    Severity = p.Severity.ToString(),
                    Status = p.Status.ToString(),
                    OccurredAt = p.OccurredAt,
                    AffectsPersonnelDecision = p.AffectsPersonnelDecision
                })
                .ToList();

            return Ok(new { Success = true, Data = data });
        }

        [HttpGet("employees/{employeeId:int}/performance-reviews")]
        public async Task<IActionResult> GetEmployeePerformanceReviews(int employeeId, CancellationToken ct)
        {
            await EnsureCanAccessEmployeeAsync(employeeId, ct);

            var reviews = await _db.PerformanceReviews
                .AsNoTracking()
                .Where(p => p.EmployeeId == employeeId)
                .OrderByDescending(p => p.FinalizedAt ?? p.CreatedAt)
                .Take(24)
                .ToListAsync(ct);

            var data = reviews.Select(p => new PersonnelChangePerformanceReviewOptionDto
                {
                    Id = p.Id,
                    Period = p.Period,
                    TotalScore = p.TotalScore,
                    FinalRating = p.FinalRating,
                    Status = p.Status.ToString(),
                    FinalizedAt = p.FinalizedAt,
                    CreatedAt = p.CreatedAt
                })
                .ToList();

            return Ok(new { Success = true, Data = data });
        }

        [HttpGet("employees/{employeeId:int}/contracts")]
        public async Task<IActionResult> GetEmployeeContracts(int employeeId, CancellationToken ct)
        {
            await EnsureCanAccessEmployeeAsync(employeeId, ct);

            var contracts = await _db.Contracts
                .AsNoTracking()
                .Where(c => c.EmployeeId == employeeId)
                .OrderByDescending(c => c.Status == ContractStatus.Active)
                .ThenByDescending(c => c.StartDate)
                .Take(20)
                .ToListAsync(ct);

            var data = contracts.Select(c => new PersonnelChangeContractOptionDto
                {
                    Id = c.Id,
                    ContractNumber = c.ContractNumber,
                    ContractType = c.ContractType.ToString(),
                    Status = c.Status.ToString(),
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    BasicSalary = c.BasicSalary,
                    InsuranceSalary = c.InsuranceSalary
                })
                .ToList();

            return Ok(new { Success = true, Data = data });
        }

        [HttpPost("evidence-files")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadEvidence([FromForm] PersonnelChangeEvidenceUploadRequestDto request, CancellationToken ct)
        {
            var file = request.File;

            if (file == null)
                return BadRequest(new { Success = false, Message = "Vui lòng chọn tệp minh chứng." });

            if (file.Length <= 0)
                return BadRequest(new { Success = false, Message = "Tệp minh chứng không hợp lệ." });

            if (file.Length > MaxEvidenceFileSize)
                return BadRequest(new { Success = false, Message = "Tệp minh chứng không được vượt quá 10MB." });

            var extension = Path.GetExtension(file.FileName);
            if (!AllowedEvidenceExtensions.Contains(extension))
                return BadRequest(new { Success = false, Message = "Chỉ hỗ trợ PDF, DOC, DOCX hoặc hình ảnh." });

            var filePath = await _storageService.UploadFileAsync(file, "personnel-change-evidence", ct);
            return Ok(new
            {
                Success = true,
                Data = new PersonnelChangeEvidenceUploadResultDto
                {
                    FilePath = filePath,
                    FileName = file.FileName,
                    Size = file.Length
                }
            });
        }

        private IQueryable<Employee> BaseEmployeeQuery() =>
            _db.Employees
                .AsNoTracking()
                .Include(e => e.Department)
                .Include(e => e.Position)
                .Include(e => e.JobLevel)
                .Include(e => e.Manager);

        private IQueryable<Employee> ApplyEmployeeScope(IQueryable<Employee> query, Employee? actorEmployee, string role)
        {
            if (IsAny(role, "Admin", "HR", "Director"))
                return query;

            if (IsManager(role))
            {
                if (actorEmployee?.DeptId.HasValue != true)
                    return query.Where(e => false);

                return query.Where(e => e.DeptId == actorEmployee!.DeptId);
            }

            if (actorEmployee != null)
                return query.Where(e => e.Id == actorEmployee.Id);

            return query.Where(e => false);
        }

        private async Task EnsureCanAccessEmployeeAsync(int employeeId, CancellationToken ct)
        {
            var actorEmployee = await GetActorEmployeeAsync(ct);
            var role = GetRole();
            var canAccess = await ApplyEmployeeScope(BaseEmployeeQuery(), actorEmployee, role)
                .AnyAsync(e => e.Id == employeeId, ct);

            if (!canAccess)
                throw new UnauthorizedAccessException("Bạn không có quyền xem dữ liệu của nhân sự này.");
        }

        private async Task<Employee?> GetActorEmployeeAsync(CancellationToken ct)
        {
            var accountId = User.GetAccountIdOrThrow();
            return await _db.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.AccountId == accountId, ct);
        }

        private static PersonnelChangeEmployeeOptionDto MapEmployeeOption(Employee employee) =>
            new()
            {
                Id = employee.Id,
                EmployeeCode = employee.EmployeeCode,
                FullName = employee.FullName,
                DepartmentId = employee.DeptId,
                DepartmentName = employee.Department?.DeptName,
                PositionId = employee.PositionId,
                PositionName = employee.Position?.Title,
                JobLevelId = employee.JobLevelId,
                JobLevelName = employee.JobLevel?.Name,
                ManagerId = employee.ManagerId,
                ManagerName = employee.Manager?.FullName,
                Status = employee.Status.ToString(),
                EmployeeType = employee.Type.ToString()
            };

        private string GetRole() =>
            User.GetRoleOrEmpty() ??
            User.FindFirst("role")?.Value ??
            User.FindFirst(ClaimTypes.Role)?.Value ??
            string.Empty;

        private static bool IsManager(string role) =>
            string.Equals(role, "Manager", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(role, "Truong phong", StringComparison.OrdinalIgnoreCase);

        private static bool IsAny(string role, params string[] values) =>
            values.Any(value => string.Equals(role, value, StringComparison.OrdinalIgnoreCase));
    }

    public class PersonnelChangeEmployeeOptionDto
    {
        public int Id { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public int? PositionId { get; set; }
        public string? PositionName { get; set; }
        public int? JobLevelId { get; set; }
        public string? JobLevelName { get; set; }
        public int? ManagerId { get; set; }
        public string? ManagerName { get; set; }
        public string Status { get; set; } = string.Empty;
        public string EmployeeType { get; set; } = string.Empty;
    }

    public class PersonnelChangeDepartmentOptionDto
    {
        public int Id { get; set; }
        public string DeptCode { get; set; } = string.Empty;
        public string DeptName { get; set; } = string.Empty;
        public int? ParentDeptId { get; set; }
        public int? ManagerId { get; set; }
        public string? ManagerName { get; set; }
    }

    public class PersonnelChangePositionOptionDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int JobLevel { get; set; }
    }

    public class PersonnelChangeJobLevelOptionDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int RankOrder { get; set; }
        public bool IsManagementLevel { get; set; }
    }

    public class PersonnelChangePenaltyOptionDto
    {
        public int Id { get; set; }
        public string Period { get; set; } = string.Empty;
        public string RuleCode { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public decimal PenaltyPoint { get; set; }
        public string Severity { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? OccurredAt { get; set; }
        public bool AffectsPersonnelDecision { get; set; }
    }

    public class PersonnelChangePerformanceReviewOptionDto
    {
        public int Id { get; set; }
        public string Period { get; set; } = string.Empty;
        public decimal TotalScore { get; set; }
        public string? FinalRating { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? FinalizedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PersonnelChangeContractOptionDto
    {
        public int Id { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public string ContractType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal BasicSalary { get; set; }
        public decimal InsuranceSalary { get; set; }
    }

    public class PersonnelChangeEvidenceUploadResultDto
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long Size { get; set; }
    }

    public class PersonnelChangeEvidenceUploadRequestDto
    {
        public IFormFile? File { get; set; }
    }
}

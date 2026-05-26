using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.Organization;
using HRM.backend.src.HRM.Core.Entities.PayrollAllowances;
using HRM.backend.src.HRM.Core.Entities.Recruitment;
using HRM.backend.src.HRM.Core.Entities.RequestHandover;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Entities.TasksTraining;
using HRM.backend.src.HRM.Core.Entities.TimeAttendance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Logging;
using System.Reflection.Emit;
using System.Security.Claims;
using System.Text.Json;

namespace HRM.backend.src.HRM.Infrastructure.Persistence
{
    public class MyDbContext : DbContext
    {
        private readonly IHttpContextAccessor? _httpContextAccessor;
        private readonly string[] SENSITIVE_FIELDS = {
            "PasswordHash", "RefreshToken", "MfaSecretKey", "MfaRecoveryCodes",
            "BaseSalary", "BonusAmount", "NetPay" // Thêm các cột tài chính nếu cần giấu IT
        };

        // Cập nhật constructor (Cho phép Nullable IHttpContextAccessor để tránh lỗi lúc chạy Migration)
        public MyDbContext(DbContextOptions<MyDbContext> options, IHttpContextAccessor? httpContextAccessor = null) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            //optionsBuilder.UseLoggerFactory(MyLoggerFactory);

            // 1. Ghi log thẳng ra Console cực nhanh, an toàn, không lo xung đột Serilog
            optionsBuilder.LogTo(
                Console.WriteLine,
                new[] { DbLoggerCategory.Database.Command.Name },
                LogLevel.Information);

            // 2. VŨ KHÍ TỐI THƯỢNG: Hiển thị giá trị thật của tham số
            optionsBuilder.EnableSensitiveDataLogging();

            // 3. Hiển thị thông tin lỗi chi tiết đến từng cột/bảng
            optionsBuilder.EnableDetailedErrors();
        }

        // ==========================================
        // DBSETS: 8 MODULES
        // ==========================================

        // 1. System
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Configuration> Configurations { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<MfaRecoveryCode> MfaRecoveryCodes { get; set; }
        public DbSet<SourceCatalog> SourceCatalogs { get; set; }
        public DbSet<SlaTrackingTask> SlaTrackingTasks { get; set; }
        public DbSet<ApprovalRequest> ApprovalRequests { get; set; }
        public DbSet<ApprovalStep> ApprovalSteps { get; set; }
        public DbSet<IdempotencyRecord> IdempotencyRecords { get; set; }
        public DbSet<OutboxMessage> OutboxMessages { get; set; }

        // 2. Organization
        public DbSet<Department> Departments { get; set; }
        public DbSet<Position> Positions { get; set; }

        // 3. Recruitment
        public DbSet<RecruitmentRequest> RecruitmentRequests { get; set; }
        public DbSet<Candidate> Candidates { get; set; }

        // 4. Employee Profile
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Dependent> Dependents { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<ContractAddendum> ContractAddendums { get; set; }
        public DbSet<OnboardingRequest> OnboardingRequests { get; set; }
        public DbSet<ProfileUpdateRequest> ProfileUpdateRequests { get; set; }

        // 5. Time & Attendance
        public DbSet<WorkShift> WorkShifts { get; set; }
        public DbSet<AttendanceLog> AttendanceLogs { get; set; }
        public DbSet<AttendanceSummary> AttendanceSummaries { get; set; }
        public DbSet<LeaveType> LeaveTypes { get; set; }
        public DbSet<LeaveBalance> LeaveBalances { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }
        public DbSet<OvertimeRequest> OvertimeRequests { get; set; }

        // 6. Tasks & Training
        public DbSet<KpiImportBatch> KpiImportBatches { get; set; }
        public DbSet<PerformanceDetail> PerformanceDetails { get; set; }
        public DbSet<PenaltyRule> PenaltyRules { get; set; }
        public DbSet<PenaltyRecord> PenaltyRecords { get; set; }
        public DbSet<WorkTask> Tasks { get; set; }
        public DbSet<PerformanceReview> PerformanceReviews { get; set; }
        public DbSet<TaskProgress> TaskProgresses { get; set; }
        public DbSet<TaskFeedback> TaskFeedbacks { get; set; }
        public DbSet<Training> Trainings { get; set; }

        // 7. Payroll & Allowances
        public DbSet<AllowanceType> AllowanceTypes { get; set; }
        public DbSet<EmployeeAllowance> EmployeeAllowances { get; set; }
        public DbSet<PayrollFormula> PayrollFormulas { get; set; }
        public DbSet<Payroll> Payrolls { get; set; }

        // 8. Requests & Handover
        public DbSet<Request> Requests { get; set; }
        public DbSet<HandoverRequest> HandoverRequests { get; set; }
        public DbSet<HandoverItem> HandoverItems { get; set; }
        public DbSet<EmploymentHistory> EmploymentHistories { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ==========================================
            // 1. KHÓA CHÍNH PHỨC HỢP (COMPOSITE KEYS)
            // ==========================================
            modelBuilder.Entity<RolePermission>()
                .HasKey(rp => new { rp.RoleId, rp.PermissionId });

            modelBuilder.Entity<LeaveBalance>()
                .HasKey(lb => new { lb.EmployeeId, lb.LeaveTypeId, lb.Year });

            // ==========================================
            // 2. UNIQUE INDEX (Đảm bảo tính duy nhất)
            // ==========================================
            modelBuilder.Entity<Account>().HasIndex(a => a.Email).IsUnique();
            modelBuilder.Entity<Role>().HasIndex(r => r.RoleName).IsUnique();
            modelBuilder.Entity<Permission>().HasIndex(p => p.PermissionCode).IsUnique();
            modelBuilder.Entity<Configuration>().HasIndex(c => new { c.ConfigGroup, c.ParamKey }).IsUnique();
            modelBuilder.Entity<SourceCatalog>().HasIndex(s => s.SourcePath).IsUnique();
            modelBuilder.Entity<IdempotencyRecord>().HasIndex(i => new { i.Scope, i.IdempotencyKey }).IsUnique();
            modelBuilder.Entity<OutboxMessage>().HasIndex(o => new { o.Status, o.CreatedAt });
            modelBuilder.Entity<Department>().HasIndex(d => d.DeptCode).IsUnique();
            modelBuilder.Entity<Position>().HasIndex(p => p.Title).IsUnique();
            modelBuilder.Entity<Contract>().HasIndex(c => c.ContractNumber).IsUnique();
            modelBuilder.Entity<ContractAddendum>().HasIndex(ca => ca.AddendumNumber).IsUnique();
            modelBuilder.Entity<ContractAddendum>().HasIndex(ca => new { ca.ContractId, ca.Status });
            modelBuilder.Entity<WorkShift>().HasIndex(s => s.DeptId).IsUnique().HasDatabaseName("UX_work_shifts_DeptId");
            modelBuilder.Entity<LeaveRequest>().HasIndex(l => new { l.EmployeeId, l.LeaveTypeId, l.StartDate, l.EndDate }).IsUnique().HasDatabaseName("UX_leave_requests_EmployeeId_LeaveTypeId_StartDate_EndDate");
            modelBuilder.Entity<OvertimeRequest>().HasIndex(o => new { o.EmployeeId, o.WorkDate, o.StartTime, o.EndTime }).IsUnique().HasDatabaseName("UX_overtime_requests_EmployeeId_WorkDate_StartTime_EndTime");
            modelBuilder.Entity<Candidate>().HasIndex(c => new { c.RecruitmentRequestId, c.Email }).IsUnique().HasDatabaseName("UX_candidates_RecruitmentRequestId_Email");
            modelBuilder.Entity<Candidate>().HasIndex(c => c.TrackingCode).IsUnique();
            modelBuilder.Entity<Employee>().HasIndex(e => e.EmployeeCode).IsUnique();
            modelBuilder.Entity<Employee>().HasIndex(e => e.AccountId).IsUnique();
            modelBuilder.Entity<Employee>().HasIndex(e => e.CandidateId).IsUnique();
            modelBuilder.Entity<Employee>().HasIndex(e => e.IdentityNumber).IsUnique();
            modelBuilder.Entity<Employee>().HasIndex(e => e.TaxCode).IsUnique();
            modelBuilder.Entity<Employee>().HasIndex(e => e.SocialInsCode).IsUnique();
            modelBuilder.Entity<PerformanceReview>().HasIndex(p => new { p.EmployeeId, p.Period }).IsUnique().HasDatabaseName("UX_performance_reviews_EmployeeId_Period");
            modelBuilder.Entity<PerformanceReview>().HasIndex(p => new { p.DeptId, p.Period, p.Status });
            modelBuilder.Entity<PerformanceDetail>().HasIndex(p => new { p.ReviewId, p.KpiCode }).IsUnique().HasDatabaseName("UX_performance_details_ReviewId_KpiCode");
            modelBuilder.Entity<PenaltyRule>().HasIndex(p => new { p.SourceType, p.RuleCode }).IsUnique().HasDatabaseName("UX_penalty_rules_SourceType_RuleCode");
            modelBuilder.Entity<PenaltyRecord>().HasIndex(p => new { p.EmployeeId, p.Period, p.SourceType });
            modelBuilder.Entity<PenaltyRecord>().HasIndex(p => new { p.SourceType, p.ReferenceId, p.RuleCode }).HasDatabaseName("IX_penalty_records_Source_Reference_Rule");
            modelBuilder.Entity<WorkTask>().HasIndex(t => new { t.AssignedTo, t.Status, t.Deadline });
            modelBuilder.Entity<TaskProgress>().HasIndex(t => new { t.TaskId, t.SubmittedAt });
            modelBuilder.Entity<Training>().HasIndex(t => new { t.EmployeeId, t.Status });
            modelBuilder.Entity<Training>().HasIndex(t => new { t.ManagerId, t.EvaluationDeadline, t.Status });

            // ==========================================
            // 3. QUAN HỆ (RELATIONSHIPS) & CHỐNG LỖI CASCADE
            // ==========================================

            // --- SYSTEM ---
            modelBuilder.Entity<Account>()
                .HasOne(a => a.Role).WithMany(r => r.Accounts)
                .HasForeignKey(a => a.RoleId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MfaRecoveryCode>()
                .HasOne<Account>()
                .WithMany()
                .HasForeignKey(m => m.AccountId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa Account -> Xóa luôn Recovery Codes

            // --- ORGANIZATION & EMPLOYEE ---
            modelBuilder.Entity<Department>()
                .HasOne(d => d.ParentDepartment).WithMany(d => d.SubDepartments)
                .HasForeignKey(d => d.ParentDeptId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Department>()
                .HasOne(d => d.Manager).WithMany()
                .HasForeignKey(d => d.ManagerId).OnDelete(DeleteBehavior.Restrict); // Tránh vòng lặp Employee -> Dept -> Manager

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Department)
                .WithMany(d => d.Employees) // Giả định class Department có ICollection<Employee> Employees
                .HasForeignKey(e => e.DeptId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Position)
                .WithMany(p => p.Employees) // Chỉ định rõ collection trong class Position
                .HasForeignKey(e => e.PositionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ContractAddendum>()
                .HasOne(ca => ca.Contract)
                .WithMany(c => c.Addendums)
                .HasForeignKey(ca => ca.ContractId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- TASKS & TRAINING ---
            modelBuilder.Entity<KpiImportBatch>()
                .HasOne(b => b.Department)
                .WithMany()
                .HasForeignKey(b => b.DeptId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<KpiImportBatch>()
                .HasOne(b => b.ImportedByAccount)
                .WithMany()
                .HasForeignKey(b => b.ImportedByAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PerformanceReview>()
                .HasOne(pr => pr.Employee)
                .WithMany()
                .HasForeignKey(pr => pr.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict); // Không cho phép xóa Employee nếu đã có điểm đánh giá (Nên dùng soft-delete cho nhân viên)

            modelBuilder.Entity<PerformanceReview>()
                .HasOne(pr => pr.Department)
                .WithMany()
                .HasForeignKey(pr => pr.DeptId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PerformanceReview>()
                .HasOne(pr => pr.ImportBatch)
                .WithMany(b => b.Reviews)
                .HasForeignKey(pr => pr.ImportBatchId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PerformanceReview>()
                .HasOne(pr => pr.CreatedByAccount)
                .WithMany()
                .HasForeignKey(pr => pr.CreatedByAccountId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PerformanceReview>()
                .HasOne(pr => pr.ReviewerAccount)
                .WithMany()
                .HasForeignKey(pr => pr.ReviewerAccountId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PerformanceReview>()
                .HasMany(pr => pr.Details)
                .WithOne(pd => pd.Review)
                .HasForeignKey(pd => pd.ReviewId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa phiếu đánh giá thì xóa luôn các dòng chi tiết KPI

            modelBuilder.Entity<PenaltyRecord>()
                .HasOne(pr => pr.Employee)
                .WithMany()
                .HasForeignKey(pr => pr.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PenaltyRecord>()
                .HasOne(pr => pr.CreatedByAccount)
                .WithMany()
                .HasForeignKey(pr => pr.CreatedByAccountId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PenaltyRecord>()
                .HasOne(pr => pr.PerformanceReview)
                .WithMany()
                .HasForeignKey(pr => pr.PerformanceReviewId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<WorkTask>()
                .HasOne(t => t.Department)
                .WithMany()
                .HasForeignKey(t => t.DeptId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkTask>()
                .HasOne(t => t.Assignee)
                .WithMany()
                .HasForeignKey(t => t.AssignedTo)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkTask>()
                .HasOne(t => t.CreatedByAccount)
                .WithMany()
                .HasForeignKey(t => t.CreatedByAccountId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<WorkTask>()
                .HasOne(t => t.Training)
                .WithMany(t => t.Tasks)
                .HasForeignKey(t => t.TrainingId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<WorkTask>()
                .HasMany(t => t.Progresses)
                .WithOne(p => p.Task)
                .HasForeignKey(p => p.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WorkTask>()
                .HasMany(t => t.Feedbacks)
                .WithOne(f => f.Task)
                .HasForeignKey(f => f.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TaskProgress>()
                .HasOne(p => p.Employee)
                .WithMany()
                .HasForeignKey(p => p.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TaskProgress>()
                .HasMany(p => p.Feedbacks)
                .WithOne(f => f.Progress)
                .HasForeignKey(f => f.ProgressId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<TaskFeedback>()
                .HasOne(f => f.Reviewer)
                .WithMany()
                .HasForeignKey(f => f.ReviewerId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Training>()
                .HasOne(t => t.Employee)
                .WithMany()
                .HasForeignKey(t => t.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Training>()
                .HasOne(t => t.Department)
                .WithMany()
                .HasForeignKey(t => t.DeptId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Training>()
                .HasOne(t => t.Manager)
                .WithMany()
                .HasForeignKey(t => t.ManagerId)
                .OnDelete(DeleteBehavior.SetNull);

            // --- REQUESTS & HANDOVER ---
            modelBuilder.Entity<HandoverRequest>()
                .HasOne(hr => hr.Sender).WithMany()
                .HasForeignKey(hr => hr.SenderId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HandoverRequest>()
                .HasOne(hr => hr.Receiver).WithMany()
                .HasForeignKey(hr => hr.ReceiverId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EmploymentHistory>()
                .HasOne(eh => eh.Approver).WithMany()
                .HasForeignKey(eh => eh.ApprovedBy).OnDelete(DeleteBehavior.SetNull);

            // --- ACCOUNT & EMPLOYEE ---
            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.AccountId)
                .IsUnique();

            // --- CANDIDATE & EMPLOYEE (khi trúng tuyển) ---
            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.CandidateId)
                .IsUnique();

            modelBuilder.Entity<OvertimeRequest>()
                .HasOne(o => o.Employee)
                .WithMany()
                .HasForeignKey(o => o.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AttendanceSummary>()
                .HasIndex(s => new { s.EmployeeId, s.Month, s.Year })
                .IsUnique();

            modelBuilder.Entity<AttendanceSummary>()
                .HasOne(s => s.Employee)
                .WithMany()
                .HasForeignKey(s => s.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Tự động chuyển đổi Enum thành String khi lưu vào DB
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    // Lấy kiểu dữ liệu gốc (xử lý cả trường hợp Enum?)
                    var type = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;

                    if (type.IsEnum)
                    {
                        // Tạo Converter phù hợp với kiểu Enum cụ thể đó
                        var converterType = typeof(EnumToStringConverter<>).MakeGenericType(type);
                        var converter = (ValueConverter)Activator.CreateInstance(converterType)!;

                        property.SetValueConverter(converter);
                        property.SetColumnType("VARCHAR(50)");
                    }
                }
            }

            foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {

                property.SetColumnType("DECIMAL(15,2)");
            }
        }
        

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var userIdClaim = _httpContextAccessor?.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            int? currentUserId = int.TryParse(userIdClaim, out int id) ? id : null;

            // Bắt các bản ghi bị thay đổi (Bỏ qua bảng AuditLog và MfaRecoveryCode)
            var modifiedEntries = ChangeTracker.Entries()
                .Where(e => e.Entity is not AuditLog && e.Entity is not MfaRecoveryCode &&
                           (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
                .ToList();

            var auditEntries = new List<AuditLog>();

            foreach (var entry in modifiedEntries)
            {
                var auditLog = new AuditLog
                {
                    TableName = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name,
                    AccountId = currentUserId,
                    ActionType = entry.State.ToString(),
                    Timestamp = DateTime.UtcNow
                };

                var oldValues = new Dictionary<string, object?>();
                var newValues = new Dictionary<string, object?>();
                var affectedColumns = new List<string>();

                foreach (var property in entry.Properties)
                {
                    if (property.IsTemporary) continue;

                    string propertyName = property.Metadata.Name;

                    // BẢO MẬT: Bỏ qua không ghi log các trường nhạy cảm
                    if (SENSITIVE_FIELDS.Contains(propertyName)) continue;

                    switch (entry.State)
                    {
                        case EntityState.Added:
                            newValues[propertyName] = property.CurrentValue;
                            affectedColumns.Add(propertyName);
                            break;

                        case EntityState.Deleted:
                            oldValues[propertyName] = property.OriginalValue;
                            affectedColumns.Add(propertyName);
                            break;

                        case EntityState.Modified:
                            // TỐI ƯU: Chỉ ghi nhận nếu giá trị thực sự có sự thay đổi
                            if (!Equals(property.OriginalValue, property.CurrentValue))
                            {
                                oldValues[propertyName] = property.OriginalValue;
                                newValues[propertyName] = property.CurrentValue;
                                affectedColumns.Add(propertyName);
                            }
                            break;
                    }
                }

                // CHỈ LƯU LOG NẾU CÓ ÍT NHẤT 1 TRƯỜNG DỮ LIỆU ĐƯỢC CẬP NHẬT
                if (affectedColumns.Count > 0)
                {
                    auditLog.OldValues = oldValues.Any() ? JsonSerializer.Serialize(oldValues) : null;
                    auditLog.NewValues = newValues.Any() ? JsonSerializer.Serialize(newValues) : null;
                    auditLog.AffectedColumns = JsonSerializer.Serialize(affectedColumns);

                    auditEntries.Add(auditLog);
                }
            }

            if (auditEntries.Any())
            {
                await AuditLogs.AddRangeAsync(auditEntries, cancellationToken);
            }

            await NormalizeAuditLogAccountIdsAsync(cancellationToken);

            return await base.SaveChangesAsync(cancellationToken);
        }

        private async Task NormalizeAuditLogAccountIdsAsync(CancellationToken cancellationToken)
        {
            var auditLogEntries = ChangeTracker.Entries<AuditLog>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
                .ToList();

            if (!auditLogEntries.Any())
                return;

            foreach (var entry in auditLogEntries.Where(e => e.Entity.AccountId.HasValue && e.Entity.AccountId.Value <= 0))
            {
                entry.Entity.AccountId = null;
            }

            var accountIds = auditLogEntries
                .Select(e => e.Entity.AccountId)
                .Where(id => id.HasValue && id.Value > 0)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            if (!accountIds.Any())
                return;

            var existingAccountIds = await Accounts
                .Where(a => accountIds.Contains(a.Id))
                .Select(a => a.Id)
                .ToListAsync(cancellationToken);

            var existingSet = existingAccountIds.ToHashSet();
            foreach (var entry in auditLogEntries.Where(e => e.Entity.AccountId.HasValue && !existingSet.Contains(e.Entity.AccountId.Value)))
            {
                entry.Entity.AccountId = null;
            }
        }
    }
}

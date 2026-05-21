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
        public DbSet<LeaveType> LeaveTypes { get; set; }
        public DbSet<LeaveBalance> LeaveBalances { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }

        // 6. Tasks & Training
        public DbSet<PerformanceDetail> PerformanceDetails { get; set; }
        public DbSet<WorkTask> Tasks { get; set; }
        public DbSet<PerformanceReview> PerformanceReviews { get; set; }
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
            modelBuilder.Entity<Department>().HasIndex(d => d.DeptCode).IsUnique();
            modelBuilder.Entity<Position>().HasIndex(p => p.Title).IsUnique();
            modelBuilder.Entity<Contract>().HasIndex(c => c.ContractNumber).IsUnique();
            modelBuilder.Entity<ContractAddendum>().HasIndex(ca => ca.AddendumNumber).IsUnique();
            modelBuilder.Entity<ContractAddendum>().HasIndex(ca => new { ca.ContractId, ca.Status });
            modelBuilder.Entity<Employee>().HasIndex(e => e.EmployeeCode).IsUnique();
            modelBuilder.Entity<Employee>().HasIndex(e => e.AccountId).IsUnique();
            modelBuilder.Entity<Employee>().HasIndex(e => e.CandidateId).IsUnique();
            modelBuilder.Entity<Employee>().HasIndex(e => e.IdentityNumber).IsUnique();
            modelBuilder.Entity<Employee>().HasIndex(e => e.TaxCode).IsUnique();
            modelBuilder.Entity<Employee>().HasIndex(e => e.SocialInsCode).IsUnique();

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
            // 1. Mối quan hệ Employee 1-N PerformanceReview
            modelBuilder.Entity<PerformanceReview>()
                .HasOne(pr => pr.Employee)
                .WithMany()
                .HasForeignKey(pr => pr.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict); // Không cho phép xóa Employee nếu đã có điểm đánh giá (Nên dùng soft-delete cho nhân viên)

            // 2. Mối quan hệ PerformanceReview 1-N PerformanceDetail
            modelBuilder.Entity<PerformanceReview>()
                .HasMany(pr => pr.Details)
                .WithOne(pd => pd.Review)
                .HasForeignKey(pd => pd.ReviewId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa phiếu đánh giá thì xóa luôn các dòng chi tiết KPI

            // 3. Mối quan hệ Employee 1-N Training
            modelBuilder.Entity<Training>()
                .HasOne(t => t.Employee)
                .WithMany()
                .HasForeignKey(t => t.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

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

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}

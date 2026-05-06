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

namespace HRM.backend.src.HRM.Infrastructure.Persistence
{
    public class MyDbContext : DbContext
    {
        public MyDbContext(DbContextOptions<MyDbContext> options) : base(options) { }

        //public static readonly ILoggerFactory MyLoggerFactory = LoggerFactory.Create(builder =>
        //{
        //    builder.AddFilter(DbLoggerCategory.Query.Name, LogLevel.Information)
        //           .SetMinimumLevel(LogLevel.Warning)
        //           .AddConsole();
        //});

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

        // 5. Time & Attendance
        public DbSet<WorkShift> WorkShifts { get; set; }
        public DbSet<AttendanceLog> AttendanceLogs { get; set; }
        public DbSet<LeaveType> LeaveTypes { get; set; }
        public DbSet<LeaveBalance> LeaveBalances { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }

        // 6. Tasks & Training
        public DbSet<DepartmentBudget> DepartmentBudgets { get; set; }
        public DbSet<WorkTask> Tasks { get; set; }
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
            modelBuilder.Entity<Department>().HasIndex(d => d.DeptCode).IsUnique();
            modelBuilder.Entity<Position>().HasIndex(p => p.Title).IsUnique();
            modelBuilder.Entity<Contract>().HasIndex(c => c.ContractNumber).IsUnique();
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

            // --- TASKS & TRAINING ---
            modelBuilder.Entity<WorkTask>()
                .HasOne(t => t.Assignee).WithMany()
                .HasForeignKey(t => t.AssignedTo).OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<TaskFeedback>()
                .HasOne(tf => tf.Reviewer).WithMany()
                .HasForeignKey(tf => tf.ReviewerId).OnDelete(DeleteBehavior.Restrict);

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
    }
}
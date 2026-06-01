using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.Services;
using HRM.backend.src.HRM.Application.Interfaces.System.Services;
using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using HRM.backend.src.HRM.Application.Services.System;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.Organization;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.PayrollAllowances;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.PersonnelChanges;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.Recruitment;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TasksTraining;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TimeAttendance;
using HRM.backend.src.HRM.Infrastructure.ExternalServices;
using HRM.backend.src.HRM.Infrastructure.Persistence.Repositories;
using HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.EmployeeProfile;
using HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.Organization;
using HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.PayrollAllowances;
using HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.PersonnelChanges;
using HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.Recruitment;
using HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.System;
using HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.TasksTraining;
using HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.TimeAttendance;

namespace HRM.backend.src.HRM.Infrastructure.Configurations
{
    public static class RepositoryConfiguration
    {
        public static IServiceCollection AddRepositoriesConfig(this IServiceCollection services)
        {
            // Đăng ký MediatR quét các Handler trong cả API và Application Assembly
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
                cfg.RegisterServicesFromAssembly(typeof(IUnitOfWork).Assembly);
            });

            // 1. Đăng ký Unit Of Work
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // 2. Đăng ký Generic Base Repository (Dùng cho mọi bảng nếu chỉ cần CRUD cơ bản)
            services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));

            // 3. Đăng ký các Repository đặc thù của 8 Module
            // System
            services.AddScoped<IAccountRepository, AccountRepository>(); 
            services.AddScoped<IAuditLogRepository, AuditLogRepository>(); 
            services.AddScoped<IConfigurationRepository, ConfigurationRepository>();
            services.AddScoped<IMfaRecoveryCodeRepository, MfaRecoveryCodeRepository>();
            services.AddScoped<ISourceCatalogRepository, SourceCatalogRepository>();
            services.AddScoped<IPayrollPolicyRepository, PayrollPolicyRepository>();
            services.AddScoped<IRbacRepository, RbacRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<ILeaveTypeRepository, LeaveTypeRepository>();
            services.AddScoped<ISlaTrackingRepository, SlaTrackingRepository>();


            // Department
            services.AddScoped<IDepartmentRepository, DepartmentRepository>();
            services.AddScoped<IPositionRepository, PositionRepository>();

            // Attendence
            services.AddScoped<IWorkShiftRepository, WorkShiftRepository>();
            services.AddScoped<IWorkCalendarConfigRepository, WorkCalendarConfigRepository>();
            services.AddScoped<IAttendanceRepository, AttendanceRepository>();
            services.AddScoped<IAttendanceSummaryRepository, AttendanceSummaryRepository>();
            services.AddScoped<IOvertimeRequestRepository, OvertimeRequestRepository>();
            services.AddScoped<ILeaveBalanceRepository, LeaveBalanceRepository>();
            services.AddScoped<ILeaveRequestRepository, LeaveRequestRepository>();

            // Profile
            services.AddScoped<IEmployeeRepository, EmployeeRepository>();
            services.AddScoped<IDependentRepository, DependentRepository>();
            services.AddScoped<IDependentUpdateRequestRepository, DependentUpdateRequestRepository>();
            services.AddScoped<IContractRepository, ContractRepository>();
            services.AddScoped<IContractAddendumRepository, ContractAddendumRepository>();
            services.AddScoped<IHistoryTrackingRepository, HistoryTrackingRepository>();

            // Recruitment
            services.AddScoped<IRecruitmentRequestRepository, RecruitmentRequestRepository>();
            services.AddScoped<ICandidateRepository, CandidateRepository>();

            // Tasks & Training
            services.AddScoped<IKpiImportBatchRepository, KpiImportBatchRepository>();
            services.AddScoped<IPerformanceReviewRepository, PerformanceReviewRepository>();
            services.AddScoped<IPerformanceDetailRepository, PerformanceDetailRepository>();
            services.AddScoped<IPenaltyRuleRepository, PenaltyRuleRepository>();
            services.AddScoped<IPenaltyRecordRepository, PenaltyRecordRepository>();
            services.AddScoped<ITaskRepository, TaskRepository>();
            services.AddScoped<ITaskProgressRepository, TaskProgressRepository>();
            services.AddScoped<ITaskFeedbackRepository, TaskFeedbackRepository>();
            services.AddScoped<ITrainingRepository, TrainingRepository>();

            // Payroll
            services.AddScoped<IPayrollRepository, PayrollRepository>();

            // Personnel Changes
            services.AddScoped<IPersonnelChangeRepository, PersonnelChangeRepository>();


            return services;
        }
    }
}

using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.EmployeeProfile.Usecases;
using HRM.backend.src.HRM.Application.Interfaces.PayrollAllowances.Services;
using HRM.backend.src.HRM.Application.Interfaces.PayrollAllowances.Usecases;
using HRM.backend.src.HRM.Application.Interfaces.PersonnelChanges.Services;
using HRM.backend.src.HRM.Application.Interfaces.PersonnelChanges.UseCases;
using HRM.backend.src.HRM.Application.Interfaces.Recruitment.Usecases;
using HRM.backend.src.HRM.Application.Interfaces.Services;
using HRM.backend.src.HRM.Application.Interfaces.System;
using HRM.backend.src.HRM.Application.Interfaces.System.Services;
using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using HRM.backend.src.HRM.Application.Interfaces.TasksTraining.Services;
using HRM.backend.src.HRM.Application.Interfaces.TasksTraining.Usecases;
using HRM.backend.src.HRM.Application.Interfaces.TimeAttendance.Services;
using HRM.backend.src.HRM.Application.Interfaces.TimeAttendance.Usecases;
using HRM.backend.src.HRM.Application.Services.System;
using HRM.backend.src.HRM.Application.Services.PayrollAllowances;
using HRM.backend.src.HRM.Application.Services.TasksTraining;
using HRM.backend.src.HRM.Application.Services.TimeAttendance;
using HRM.backend.src.HRM.Application.UseCases.EmployeeProfile;
using HRM.backend.src.HRM.Application.UseCases.PayrollAllowances;
using HRM.backend.src.HRM.Application.UseCases.PersonnelChanges;
using HRM.backend.src.HRM.Application.UseCases.Recruitment;
using HRM.backend.src.HRM.Application.UseCases.System;
using HRM.backend.src.HRM.Application.UseCases.TasksTraining;
using HRM.backend.src.HRM.Application.UseCases.TimeAttendance;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Infrastructure.ExternalServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HRM.backend.src.HRM.Infrastructure.Configurations
{
    public static class ServiceConfiguration
    {
        public static IServiceCollection AddServicesConfig(this IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            // UseCases chứa business logic -> Scoped
            // Module: System
            services.AddScoped<IIdentityUseCase, IdentityUseCase>();
            services.AddScoped<ISalaryVariableUseCase, SalaryVariableUseCase>();
            services.AddScoped<ISourceCatalogUseCase, SourceCatalogUseCase>();
            services.AddScoped<ISlaManagementUseCase, SlaManagementUseCase>(); 
            services.AddScoped<IRbacUseCase, RbacUseCase>();
            services.AddScoped<IAuditManagementUseCase, AuditManagementUseCase>();
            services.AddScoped<IAccountManagementUseCase, AccountManagementUseCase>();
            services.AddScoped<IAttendanceConfigUseCase, AttendanceConfigUseCase>();
            services.AddScoped<ILeaveTypeUseCase, LeaveTypeUseCase>();
            services.AddScoped<IDocumentExportUseCase, DocumentExportUseCase>();
            services.AddScoped<IPayrollPolicyUseCase, PayrollPolicyUseCase>();
            services.AddScoped<ISlaTrackingService, SlaTrackingService>();
            services.AddScoped<IApprovalConflictGuard, ApprovalConflictGuard>();
            services.AddScoped<IIdempotencyService, IdempotencyService>();
            

            // Module: Department
            services.AddScoped<IOrgTreeUseCase, OrgTreeUseCase>();

            // Module: TimeAttendance
            services.AddScoped<IShiftManagementUseCase, ShiftManagementUseCase>();
            services.AddScoped<IAttendanceUseCase, AttendanceUseCase>();
            services.AddScoped<IAttendanceSummaryUseCase, AttendanceSummaryUseCase>();
            services.AddScoped<IOvertimeRequestUseCase, OvertimeRequestUseCase>();
            services.AddScoped<IOvertimeReconciliationService, OvertimeReconciliationService>();
            services.AddScoped<IAttendancePenaltyGeneratorService, AttendancePenaltyGeneratorService>();
            services.AddScoped<ILeaveRequestUseCase, LeaveRequestUseCase>();

            // Module: Performance & Training
            services.AddScoped<IKpiManagementUseCase, KpiManagementUseCase>();
            services.AddScoped<ITaskManagementUseCase, TaskManagementUseCase>();
            services.AddScoped<IPerformanceEvaluationUseCase, PerformanceEvaluationUseCase>();
            services.AddScoped<IPenaltyManagementUseCase, PenaltyManagementUseCase>();
            services.AddScoped<ITrainingUseCase, TrainingUseCase>();
            services.AddScoped<IExcelKpiParserService, ExcelKpiParserService>();

            // Module: Payroll
            services.AddScoped<IPayrollSourceResolver, PayrollSourceResolver>();
            services.AddScoped<IPayrollFormulaValidator, PayrollFormulaValidator>();
            services.AddScoped<IPayrollCalculationEngine, PayrollCalculationEngine>();
            services.AddScoped<IPayrollSnapshotWriter, PayrollSnapshotWriter>();
            services.AddScoped<IPayrollCalculationUseCase, PayrollCalculationUseCase>();
            services.AddScoped<IPayrollAccessUseCase, PayrollAccessUseCase>();

            // Module: Profile
            services.AddScoped<IManageProfileUseCase, ManageProfileUseCase>();
            services.AddScoped<IDependentUseCase, DependentUseCase>();
            services.AddScoped<IHistoryTrackingUseCase, HistoryTrackingUseCase>();

            // Module: Recruitment
            services.AddScoped<ICandidateUseCase, CandidateUseCase>();
            services.AddScoped<IRecruitmentUseCase, RecruitmentUseCase>();
            services.AddScoped<IPositionUseCase, PositionUseCase>();
            services.AddScoped<IOnboardingUseCase, OnboardingUseCase>();
            services.AddScoped<IContractUseCase, ContractUseCase>();
            services.AddScoped<IContractAddendumUseCase, ContractAddendumUseCase>();

            // Module: Personnel Changes
            services.AddScoped<IPersonnelChangeUseCase, PersonnelChangeUseCase>();
            services.AddScoped<IPromotionOfficialUseCase, PromotionOfficialUseCase>();
            services.AddScoped<ISeniorAppointmentUseCase, SeniorAppointmentUseCase>();
            services.AddScoped<IVoluntaryTerminationUseCase, VoluntaryTerminationUseCase>();
            services.AddScoped<IDismissalDisciplinaryUseCase, DismissalDisciplinaryUseCase>();
            services.AddScoped<IInternalTransferUseCase, InternalTransferUseCase>();
            services.AddScoped<PersonnelChangeRiskSummaryBuilder>();
            services.AddScoped<IPersonnelChangeContractFlowService, PersonnelChangeContractFlowService>();

            // Các service có trạng thái nội bộ liên quan đến request -> Scoped
            services.AddScoped<IJwtService, Infrastructure.ExternalServices.JwtService>(); // Đảm bảo mapping đúng namespace

            // HttpClient cho Google OAuth
            services.AddHttpClient<IGoogleOAuthService, GoogleOAuthService>();

            services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<INotificationTemplateRenderer, NotificationTemplateRenderer>();


            // MfaService thuần tính toán (stateless) -> Transient hoặc Singleton đều được
            services.AddTransient<IMfaService, MfaService>();

            // Hỗ trợ lưu trữ
            services.AddScoped<IStorageService, LocalStorageService>();

            // Đăng ký Worker chạy ngầm
            services.AddHostedService<CentralSlaWorker>();
            services.AddHostedService<TaskSlaWorker>();
            services.AddHostedService<TrainingSlaWorker>();
            services.AddHostedService<PersonnelChangeSlaWorker>();

            // Đăng ký Event
            // Đăng ký MediatR (Tự động quét và đăng ký tất cả các Event Handler trong Assembly)            
            services.AddScoped<IApprovalWorkflowService, ApprovalWorkflowService>();

            return services;
        }
    }
}

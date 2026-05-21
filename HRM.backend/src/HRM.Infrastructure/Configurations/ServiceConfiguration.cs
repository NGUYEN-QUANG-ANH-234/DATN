using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.EmployeeProfile.Usecases;
using HRM.backend.src.HRM.Application.Interfaces.Recruitment.Usecases;
using HRM.backend.src.HRM.Application.Interfaces.Services;
using HRM.backend.src.HRM.Application.Interfaces.System;
using HRM.backend.src.HRM.Application.Interfaces.System.Services;
using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using HRM.backend.src.HRM.Application.Interfaces.TimeAttendance.Usecases;
using HRM.backend.src.HRM.Application.Services.System;
using HRM.backend.src.HRM.Application.UseCases.EmployeeProfile;
using HRM.backend.src.HRM.Application.UseCases.Recruitment;
using HRM.backend.src.HRM.Application.UseCases.System;
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
            services.AddScoped<ISlaTrackingService, SlaTrackingService>();
            

            // Module: Department
            services.AddScoped<IOrgTreeUseCase, OrgTreeUseCase>();

            // Module: TimeAttendance
            services.AddScoped<IShiftManagementUseCase, ShiftManagementUseCase>();
            services.AddScoped<IAttendanceUseCase, AttendanceUseCase>();

            // Module: Profile
            services.AddScoped<IManageProfileUseCase, ManageProfileUseCase>();
            services.AddScoped<IHistoryTrackingUseCase, HistoryTrackingUseCase>();

            // Module: Recruitment
            services.AddScoped<ICandidateUseCase, CandidateUseCase>();
            services.AddScoped<IRecruitmentUseCase, RecruitmentUseCase>();
            services.AddScoped<IPositionUseCase, PositionUseCase>();
            services.AddScoped<IOnboardingUseCase, OnboardingUseCase>();
            services.AddScoped<IContractUseCase, ContractUseCase>();
            services.AddScoped<IContractAddendumUseCase, ContractAddendumUseCase>();

            // Các service có trạng thái nội bộ liên quan đến request -> Scoped
            services.AddScoped<IJwtService, Infrastructure.ExternalServices.JwtService>(); // Đảm bảo mapping đúng namespace

            // HttpClient cho Google OAuth
            services.AddHttpClient<IGoogleOAuthService, GoogleOAuthService>();

            services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
            services.AddScoped<IEmailService, EmailService>();


            // MfaService thuần tính toán (stateless) -> Transient hoặc Singleton đều được
            services.AddTransient<IMfaService, MfaService>();

            // Hỗ trợ lưu trữ
            services.AddScoped<IStorageService, LocalStorageService>();

            // Đăng ký Worker chạy ngầm
            services.AddHostedService<CentralSlaWorker>();

            // Đăng ký Event
            // Đăng ký MediatR (Tự động quét và đăng ký tất cả các Event Handler trong Assembly)            
            services.AddScoped<IApprovalWorkflowService, ApprovalWorkflowService>();

            return services;
        }
    }
}

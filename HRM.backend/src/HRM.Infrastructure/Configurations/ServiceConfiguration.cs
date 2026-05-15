using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.Interfaces.Services;
using HRM.backend.src.HRM.Application.Interfaces.System;
using HRM.backend.src.HRM.Application.Interfaces.System.Services;
using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using HRM.backend.src.HRM.Application.Services.System;
using HRM.backend.src.HRM.Application.UseCases.System;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Infrastructure.ExternalServices;

namespace HRM.backend.src.HRM.Infrastructure.Configurations
{
    public static class ServiceConfiguration
    {
        public static IServiceCollection AddServicesConfig(this IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            // UseCases chứa business logic -> Scoped
            // Module 1: System
            services.AddScoped<IIdentityUseCase, IdentityUseCase>();
            services.AddScoped<ISalaryVariableUseCase, SalaryVariableUseCase>();
            services.AddScoped<ISourceCatalogUseCase, SourceCatalogUseCase>();
            services.AddScoped<ISlaManagementUseCase, SlaManagementUseCase>(); 
            services.AddScoped<IRbacUseCase, RbacUseCase>();
            services.AddScoped<IAuditManagementUseCase, AuditManagementUseCase>();
            services.AddScoped<IAccountManagementUseCase, AccountManagementUseCase>();

            // Các service có trạng thái nội bộ liên quan đến request -> Scoped
            services.AddScoped<IJwtService, Infrastructure.ExternalServices.JwtService>(); // Đảm bảo mapping đúng namespace

            // HttpClient cho Google OAuth
            services.AddHttpClient<IGoogleOAuthService, GoogleOAuthService>();

            services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
            services.AddScoped<IEmailService, EmailService>();


            // MfaService thuần tính toán (stateless) -> Transient hoặc Singleton đều được
            services.AddTransient<IMfaService, MfaService>();

            return services;
        }
    }
}

using HRM.backend.src.HRM.Application.Interfaces.Services;
using HRM.backend.src.HRM.Application.Interfaces.System.Services;
using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using HRM.backend.src.HRM.Application.Services.System;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System.HRM.backend.src.HRM.Infrastructure.Repositories.Interfaces.System;
using HRM.backend.src.HRM.Infrastructure.Persistence.Repositories;
using HRM.backend.src.HRM.Infrastructure.Persistence.Repositories.System;

namespace HRM.backend.src.HRM.Infrastructure.Configurations
{
    public static class RepositoryConfiguration
    {
        public static IServiceCollection AddRepositoriesConfig(this IServiceCollection services)
        {
            // 1. Đăng ký Unit Of Work
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // 2. Đăng ký Generic Base Repository (Dùng cho mọi bảng nếu chỉ cần CRUD cơ bản)
            services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));

            // 3. Đăng ký các Repository đặc thù của 8 Module
            // Module 1: System
            services.AddScoped<IAccountRepository, AccountRepository>(); 
            services.AddScoped<IAuditLogRepository, AuditLogRepository>(); 
            services.AddScoped<IConfigurationRepository, ConfigurationRepository>();
            services.AddScoped<IMfaRecoveryCodeRepository, MfaRecoveryCodeRepository>();
            services.AddScoped<ISourceCatalogRepository, SourceCatalogRepository>();
            services.AddScoped<IRbacRepository, RbacRepository>();
            services.AddScoped<IAuditLogRepository, AuditLogRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();



            return services;
        }
    }
}

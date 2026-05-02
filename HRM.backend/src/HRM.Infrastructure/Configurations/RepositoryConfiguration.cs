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
            services.AddScoped<IAccountRepository, AccountRepository>(); 
            services.AddScoped<IAuditLogRepository, AuditLogRepository>(); 
            services.AddScoped<IConfigurationRepository, ConfigurationRepository>();

            // Đừng quên những dòng này ở Program.cs hoặc file Config tương ứng
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<IIdentityUseCase, IdentityUseCase>();
            services.AddHttpClient<IGoogleOAuthService, GoogleOAuthService>();
            services.AddScoped<IMfaService, MfaService>();

            return services;
        }
    }
}

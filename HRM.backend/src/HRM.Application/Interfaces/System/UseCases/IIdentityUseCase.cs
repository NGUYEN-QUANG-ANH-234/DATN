using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.Interfaces.Services;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System.HRM.backend.src.HRM.Infrastructure.Repositories.Interfaces.System;

namespace HRM.backend.src.HRM.Application.Interfaces.System.UseCases
{
    public interface IIdentityUseCase
    {
        Task<AuthResponseDto> ProcessOAuthLoginAsync(string authCode);
        Task<AuthResponseDto> VerifyMfaLoginAsync(string otpCode, string tempToken);
    }
}

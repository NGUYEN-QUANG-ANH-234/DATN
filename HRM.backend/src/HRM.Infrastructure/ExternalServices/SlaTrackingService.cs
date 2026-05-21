using HRM.backend.src.HRM.Application.Interfaces;
using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;

namespace HRM.backend.src.HRM.Infrastructure.ExternalServices
{
    public class SlaTrackingService : ISlaTrackingService
    {
        private readonly ISlaTrackingRepository _slaRepo; // ĐÃ SỬA THÀNH REPO CHUYÊN BIỆT
        private readonly ISlaManagementUseCase _slaConfigUseCase;

        public SlaTrackingService(
            ISlaTrackingRepository slaRepo,
            ISlaManagementUseCase slaConfigUseCase)
        {
            _slaRepo = slaRepo;
            _slaConfigUseCase = slaConfigUseCase;
        }

        public async Task CreateTaskAsync(SlaModuleType module, int referenceId, CancellationToken ct = default)
        {
            var configs = await _slaConfigUseCase.GetSLAConfigsAsync(ct);
            var moduleConfig = configs.FirstOrDefault(c => c.ModuleCode == module.ToString());

            int timeValue;
            string unit;

            if (moduleConfig == null)
            {
                unit = "DAYS";
                switch (module)
                {
                    case SlaModuleType.ContractRenewal:
                        timeValue = 3;
                        break;
                    case SlaModuleType.DirectorContractApproval:
                        timeValue = 2;
                        break;
                    case SlaModuleType.CandidateApproval:
                        timeValue = 3;
                        break;
                    case SlaModuleType.Onboarding:
                        timeValue = 5;
                        break;
                    default:
                        timeValue = 24;
                        unit = "HOURS";
                        break;
                }
            }
            else
            {
                timeValue = int.Parse(moduleConfig.Value);
                unit = moduleConfig.Unit.ToUpper();
            }

            DateTime deadline = unit == "DAYS"
                ? DateTime.UtcNow.AddDays(timeValue)
                : DateTime.UtcNow.AddHours(timeValue);

            var task = new SlaTrackingTask
            {
                ModuleType = module,
                ReferenceId = referenceId,
                Deadline = deadline,
                Status = SlaTaskStatus.Pending
            };

            await _slaRepo.AddAsync(task, ct);
        }

        public async Task ResolveTaskAsync(SlaModuleType module, int referenceId, CancellationToken ct = default)
        {
            // SỬ DỤNG HÀM TỪ REPO RIÊNG BIỆT (Gọn gàng hơn rất nhiều)
            var task = await _slaRepo.GetPendingTaskAsync(module, referenceId, ct);

            if (task != null)
            {
                task.Status = SlaTaskStatus.Resolved;
                task.ResolvedAt = DateTime.UtcNow;
                await _slaRepo.UpdateAsync(task, ct);
            }
        }
    }
}

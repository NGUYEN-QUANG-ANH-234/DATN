using HRM.backend.src.HRM.Application.DTOs.TasksTraining;
using Microsoft.AspNetCore.Http;

namespace HRM.backend.src.HRM.Application.Interfaces.TasksTraining.Services
{
    public interface IExcelKpiParserService
    {
        Task<List<KpiImportRowDto>> ParseToDtoListAsync(IFormFile file, CancellationToken ct = default);
    }
}

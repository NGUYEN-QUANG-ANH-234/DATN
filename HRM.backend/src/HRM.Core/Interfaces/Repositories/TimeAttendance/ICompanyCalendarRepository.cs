using HRM.backend.src.HRM.Core.Entities.TimeAttendance;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.TimeAttendance
{
    public interface ICompanyCalendarRepository : IBaseRepository<CompanyCalendar>
    {
        Task<CompanyCalendar?> GetByIdWithDaysAsync(int id, CancellationToken ct = default);
        Task<List<CompanyCalendar>> GetByYearAsync(short year, CancellationToken ct = default);
        Task<CompanyCalendar?> GetActiveByYearAsync(short year, CancellationToken ct = default);
        Task<CompanyCalendar?> GetActiveByDateAsync(DateTime date, CancellationToken ct = default);
    }
}

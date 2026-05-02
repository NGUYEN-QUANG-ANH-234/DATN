namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.TimeAttendance
{
    public interface ILeaveBalanceRepository
    {
        Task<decimal> GetAvailableBalanceAsync(int empId, int leaveTypeId, short year);
        Task DeductLeaveBalanceAsync(int empId, int leaveTypeId, short year, decimal days);
    }
}

using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.TimeAttendance;

namespace HRM.backend.src.HRM.Infrastructure.ExternalServices
{
    public class LeaveSlaWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LeaveSlaWorker> _logger;

        public LeaveSlaWorker(IServiceProvider serviceProvider, ILogger<LeaveSlaWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var leaveReqRepo = scope.ServiceProvider.GetRequiredService<ILeaveRequestRepository>();
                    var leaveBalRepo = scope.ServiceProvider.GetRequiredService<ILeaveBalanceRepository>();
                    var attendanceRepo = scope.ServiceProvider.GetRequiredService<IAttendanceRepository>();
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                    var expiredRequests = (await leaveReqRepo.FetchExpiredRequestsAsync()).ToList();
                    foreach (var request in expiredRequests)
                    {
                        if (request.Status == LeaveRequestStatus.PendingDept)
                        {
                            request.Status = LeaveRequestStatus.PendingHR;
                            request.DeadlineAt = DateTime.UtcNow.AddHours(24);
                            await leaveReqRepo.UpdateAsync(request, stoppingToken);
                            continue;
                        }

                        if (request.Status != LeaveRequestStatus.PendingDirector ||
                            request.EmployeeId == null ||
                            request.LeaveTypeId == null ||
                            request.StartDate == null ||
                            request.EndDate == null)
                        {
                            continue;
                        }

                        request.Status = LeaveRequestStatus.AutoFinalApproved;
                        request.DeadlineAt = null;

                        var days = CountBusinessDays(request.StartDate.Value, request.EndDate.Value);
                        if (request.LeaveType?.IsPaid == true)
                        {
                            var balance = await leaveBalRepo.GetBalanceAsync(
                                request.EmployeeId.Value,
                                request.LeaveTypeId.Value,
                                (short)request.StartDate.Value.Year,
                                stoppingToken);

                            if (balance != null)
                                balance.UsedDays = (balance.UsedDays ?? 0) + days;
                        }

                        await attendanceRepo.SyncLeaveToAttendanceAsync(
                            request.EmployeeId.Value,
                            EnumerateBusinessDates(request.StartDate.Value, request.EndDate.Value).ToList(),
                            AttendanceStatus.OnLeave);

                        await leaveReqRepo.UpdateAsync(request, stoppingToken);
                    }

                    if (expiredRequests.Count > 0)
                        await unitOfWork.CommitAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Leave SLA worker cycle failed.");
                }

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private static decimal CountBusinessDays(DateTime startDate, DateTime endDate)
        {
            return EnumerateBusinessDates(startDate, endDate).Count();
        }

        private static IEnumerable<DateTime> EnumerateBusinessDates(DateTime startDate, DateTime endDate)
        {
            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                if (date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
                    yield return date;
            }
        }
    }
}

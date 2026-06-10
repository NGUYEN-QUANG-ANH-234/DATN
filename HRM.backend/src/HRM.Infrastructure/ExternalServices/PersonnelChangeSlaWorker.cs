using HRM.backend.src.HRM.Application.DTOs.System;
using HRM.backend.src.HRM.Application.Interfaces.System.Services;
using HRM.backend.src.HRM.Application.Interfaces.System.UseCases;
using HRM.backend.src.HRM.Application.UseCases.PersonnelChanges;
using HRM.backend.src.HRM.Core.Entities.PersonnelChanges;
using HRM.backend.src.HRM.Core.Entities.System;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.PersonnelChanges;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.System;
using HRM.backend.src.HRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HRM.backend.src.HRM.Infrastructure.ExternalServices
{
    public class PersonnelChangeSlaWorker : BackgroundService
    {
        private static readonly PersonnelChangeStatus[] WatchedStatuses =
        {
            PersonnelChangeStatus.PendingHRReview,
            PersonnelChangeStatus.PendingDirectorApproval,
            PersonnelChangeStatus.PendingEmployeeConsent,
            PersonnelChangeStatus.PendingContractFlow,
            PersonnelChangeStatus.ContractNegotiating
        };

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PersonnelChangeSlaWorker> _logger;
        private readonly IConfiguration _configuration;

        public PersonnelChangeSlaWorker(
            IServiceProvider serviceProvider,
            ILogger<PersonnelChangeSlaWorker> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var options = PersonnelChangeSlaOptions.FromConfiguration(_configuration);

                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<MyDbContext>();
                    var personnelChangeRepo = scope.ServiceProvider.GetRequiredService<IPersonnelChangeRepository>();
                    var auditLogRepo = scope.ServiceProvider.GetRequiredService<IAuditLogRepository>();
                    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                    var slaUseCase = scope.ServiceProvider.GetRequiredService<ISlaManagementUseCase>();
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                    var now = DateTime.UtcNow;
                    var roleEmails = await LoadRoleEmailsAsync(dbContext, stoppingToken);
                    var slaConfigs = (await slaUseCase.GetSLAConfigsAsync(stoppingToken))
                        .ToDictionary(config => config.ModuleCode, StringComparer.OrdinalIgnoreCase);
                    var overdueRequests = await dbContext.PersonnelChangeRequests
                        .Include(request => request.Employee)
                            .ThenInclude(employee => employee!.Account)
                        .Include(request => request.RequestedByAccount)
                        .Include(request => request.HRAssignedAccount)
                        .Include(request => request.Histories)
                        .Where(request => WatchedStatuses.Contains(request.Status))
                        .ToListAsync(stoppingToken);

                    var changed = false;
                    foreach (var request in overdueRequests)
                    {
                        if (AlreadyEscalatedForCurrentStatus(request))
                            continue;

                        var statusEnteredAt = ResolveStatusEnteredAt(request);
                        var thresholdHours = ResolveThresholdHours(options, slaConfigs, request.Status);
                        if (!thresholdHours.HasValue)
                            continue;

                        if (statusEnteredAt.AddHours(thresholdHours.Value) > now)
                            continue;

                        await NotifyAsync(emailService, request, roleEmails, statusEnteredAt, thresholdHours.Value);
                        await EscalateAsync(
                            personnelChangeRepo,
                            auditLogRepo,
                            request,
                            statusEnteredAt,
                            thresholdHours.Value,
                            options.SetStatusEscalated,
                            stoppingToken);
                        changed = true;
                    }

                    if (changed)
                        await unitOfWork.CommitAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Personnel change SLA worker cycle failed.");
                }

                await Task.Delay(TimeSpan.FromMinutes(Math.Max(5, options.ScanIntervalMinutes)), stoppingToken);
            }
        }

        private static async Task EscalateAsync(
            IPersonnelChangeRepository personnelChangeRepo,
            IAuditLogRepository auditLogRepo,
            PersonnelChangeRequest request,
            DateTime statusEnteredAt,
            int thresholdHours,
            bool setStatusEscalated,
            CancellationToken ct)
        {
            var oldStatus = request.Status;
            var note = BuildEscalationNote(request, statusEnteredAt, thresholdHours);

            await personnelChangeRepo.AddHistoryAsync(new PersonnelChangeHistory
            {
                RequestId = request.Id,
                Action = PersonnelChangeStatusGuard.SlaEscalatedAction,
                OldStatus = oldStatus,
                NewStatus = setStatusEscalated ? PersonnelChangeStatus.Escalated : oldStatus,
                ActorAccountId = null,
                Note = note,
                CreatedAt = DateTime.UtcNow
            }, ct);

            await auditLogRepo.LogSystemEventAsync(
                "PersonnelChangeSlaEscalated",
                null,
                "personnel_change_requests",
                $"Request #{request.Id} status {oldStatus} exceeded SLA {thresholdHours}h. Notify and escalate only.");

            if (!setStatusEscalated)
                return;

            request.Status = PersonnelChangeStatus.Escalated;
            request.UpdatedAt = DateTime.UtcNow;
            personnelChangeRepo.Update(request);
        }

        private static async Task NotifyAsync(
            IEmailService emailService,
            PersonnelChangeRequest request,
            RoleEmailLookup roleEmails,
            DateTime statusEnteredAt,
            int thresholdHours)
        {
            var recipients = ResolveRecipients(request, roleEmails);
            if (recipients.Count == 0)
                return;

            var subject = $"[SLA ESCALATION] Personnel change #{request.Id} overdue";
            var body = $"""
                <h3>Personnel change SLA escalation</h3>
                <p>Request <b>#{request.Id}</b> is still in <b>{request.Status}</b>.</p>
                <p>Employee: <b>{request.Employee?.FullName ?? "N/A"}</b></p>
                <p>SLA threshold: <b>{thresholdHours} hours</b>. Status entered at: <b>{statusEnteredAt:yyyy-MM-dd HH:mm:ss} UTC</b>.</p>
                <p>This notification only escalates visibility. It does not auto approve, auto terminate, or auto transfer.</p>
                """;

            foreach (var recipient in recipients)
            {
                await emailService.SendEmailAsync(recipient, subject, body);
            }
        }

        private static HashSet<string> ResolveRecipients(PersonnelChangeRequest request, RoleEmailLookup roleEmails)
        {
            var recipients = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void Add(string? email)
            {
                if (!string.IsNullOrWhiteSpace(email))
                    recipients.Add(email.Trim());
            }

            if (request.Status == PersonnelChangeStatus.PendingEmployeeConsent)
                Add(request.Employee?.Account?.Email);

            if (request.Status == PersonnelChangeStatus.PendingDirectorApproval)
            {
                foreach (var email in roleEmails.DirectorEmails)
                    Add(email);
            }
            else
            {
                foreach (var email in roleEmails.HrEmails)
                    Add(email);

                foreach (var email in roleEmails.DirectorEmails)
                    Add(email);
            }

            Add(request.HRAssignedAccount?.Email);
            Add(request.RequestedByAccount?.Email);

            return recipients;
        }

        private static async Task<RoleEmailLookup> LoadRoleEmailsAsync(MyDbContext dbContext, CancellationToken ct)
        {
            var accounts = await dbContext.Accounts
                .Include(account => account.Role)
                .Where(account => account.Status == AccountStatus.Active)
                .AsNoTracking()
                .ToListAsync(ct);

            return new RoleEmailLookup(
                FindEmailsByRole(accounts, "HR"),
                FindEmailsByRole(accounts, "Director"));
        }

        private static List<string> FindEmailsByRole(List<Account> accounts, string roleName)
        {
            return accounts
                .Where(account => account.Role.RoleName.Contains(roleName, StringComparison.OrdinalIgnoreCase))
                .Select(account => account.Email)
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool AlreadyEscalatedForCurrentStatus(PersonnelChangeRequest request)
        {
            return request.Histories.Any(history =>
                history.Action == PersonnelChangeStatusGuard.SlaEscalatedAction &&
                history.OldStatus == request.Status);
        }

        private static DateTime ResolveStatusEnteredAt(PersonnelChangeRequest request)
        {
            var history = request.Histories
                .Where(item => item.NewStatus == request.Status)
                .OrderByDescending(item => item.CreatedAt)
                .FirstOrDefault();

            if (history != null)
                return history.CreatedAt;

            return request.Status == PersonnelChangeStatus.PendingHRReview
                ? request.RequestedAt
                : request.UpdatedAt;
        }

        private static int? ResolveThresholdHours(
            PersonnelChangeSlaOptions options,
            IReadOnlyDictionary<string, SlaDto> configs,
            PersonnelChangeStatus status)
        {
            var code = ResolveSlaCode(status);
            if (code != null && configs.TryGetValue(code, out var config))
            {
                if (!config.IsActive)
                    return null;

                if (int.TryParse(config.Value, out var value) && value > 0)
                    return string.Equals(config.Unit, "DAYS", StringComparison.OrdinalIgnoreCase)
                        ? value * 24
                        : value;
            }

            return options.GetThresholdHours(status);
        }

        private static string? ResolveSlaCode(PersonnelChangeStatus status)
        {
            return status switch
            {
                PersonnelChangeStatus.PendingHRReview => "PersonnelChangeHrReview",
                PersonnelChangeStatus.PendingDirectorApproval => "PersonnelChangeDirectorApproval",
                PersonnelChangeStatus.PendingEmployeeConsent => "PersonnelChangeEmployeeConsent",
                PersonnelChangeStatus.PendingContractFlow => "PersonnelChangeContractFlow",
                PersonnelChangeStatus.ContractNegotiating => "PersonnelChangeContractFlow",
                PersonnelChangeStatus.PendingDecisionIssuance => "PersonnelChangeDecisionIssuance",
                _ => null
            };
        }

        private static string BuildEscalationNote(
            PersonnelChangeRequest request,
            DateTime statusEnteredAt,
            int thresholdHours)
        {
            return $"SLA exceeded for {request.Status}. Status entered at {statusEnteredAt:yyyy-MM-dd HH:mm:ss} UTC. Threshold: {thresholdHours}h. Notify/escalate/audit only.";
        }

        private sealed record RoleEmailLookup(List<string> HrEmails, List<string> DirectorEmails);

        private sealed class PersonnelChangeSlaOptions
        {
            public int ScanIntervalMinutes { get; init; } = 60;
            public bool SetStatusEscalated { get; init; } = true;
            public int PendingHRReviewHours { get; init; } = 48;
            public int PendingDirectorApprovalHours { get; init; } = 48;
            public int PendingEmployeeConsentHours { get; init; } = 72;
            public int PendingContractFlowHours { get; init; } = 72;

            public int GetThresholdHours(PersonnelChangeStatus status)
            {
                return status switch
                {
                    PersonnelChangeStatus.PendingHRReview => PendingHRReviewHours,
                    PersonnelChangeStatus.PendingDirectorApproval => PendingDirectorApprovalHours,
                    PersonnelChangeStatus.PendingEmployeeConsent => PendingEmployeeConsentHours,
                    PersonnelChangeStatus.PendingContractFlow => PendingContractFlowHours,
                    PersonnelChangeStatus.ContractNegotiating => PendingContractFlowHours,
                    _ => 72
                };
            }

            public static PersonnelChangeSlaOptions FromConfiguration(IConfiguration configuration)
            {
                var section = configuration.GetSection("PersonnelChange:Sla");

                return new PersonnelChangeSlaOptions
                {
                    ScanIntervalMinutes = GetPositiveInt(section, "ScanIntervalMinutes", 60),
                    SetStatusEscalated = section.GetValue("SetStatusEscalated", true),
                    PendingHRReviewHours = GetPositiveInt(section, "PendingHRReviewHours", 48),
                    PendingDirectorApprovalHours = GetPositiveInt(section, "PendingDirectorApprovalHours", 48),
                    PendingEmployeeConsentHours = GetPositiveInt(section, "PendingEmployeeConsentHours", 72),
                    PendingContractFlowHours = GetPositiveInt(section, "PendingContractFlowHours", 72)
                };
            }

            private static int GetPositiveInt(IConfiguration section, string key, int defaultValue)
            {
                var value = section.GetValue(key, defaultValue);
                return value > 0 ? value : defaultValue;
            }
        }
    }
}

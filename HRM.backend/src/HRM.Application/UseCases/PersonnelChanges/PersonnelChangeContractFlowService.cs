using System.Text.Json;
using HRM.backend.src.HRM.Application.DTOs.Events;
using HRM.backend.src.HRM.Application.Interfaces.PersonnelChanges.Services;
using HRM.backend.src.HRM.Core.Entities.EmployeeProfile;
using HRM.backend.src.HRM.Core.Entities.PersonnelChanges;
using HRM.backend.src.HRM.Core.Enums;
using HRM.backend.src.HRM.Core.Interfaces.Repositories;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.EmployeeProfile;
using HRM.backend.src.HRM.Core.Interfaces.Repositories.PersonnelChanges;
using MediatR;

namespace HRM.backend.src.HRM.Application.UseCases.PersonnelChanges
{
    public class PersonnelChangeContractFlowService : IPersonnelChangeContractFlowService
    {
        private readonly IPersonnelChangeRepository _personnelChangeRepo;
        private readonly IContractRepository _contractRepo;
        private readonly IContractAddendumRepository _contractAddendumRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;

        private static readonly HashSet<string> AcceptedStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "Accepted",
            "Signed"
        };

        public PersonnelChangeContractFlowService(
            IPersonnelChangeRepository personnelChangeRepo,
            IContractRepository contractRepo,
            IContractAddendumRepository contractAddendumRepo,
            IUnitOfWork unitOfWork,
            IMediator mediator)
        {
            _personnelChangeRepo = personnelChangeRepo;
            _contractRepo = contractRepo;
            _contractAddendumRepo = contractAddendumRepo;
            _unitOfWork = unitOfWork;
            _mediator = mediator;
        }

        public PersonnelChangeStatus ResolveAfterDirectorApproval(PersonnelChangeRequest request)
        {
            if (request.RequiresContractFlow)
                return PersonnelChangeStatus.PendingContractFlow;

            return PersonnelChangeStatus.PendingDecisionIssuance;
        }

        public bool IsContractFlowCompleted(PersonnelChangeRequest request)
        {
            if (!request.RequiresContractFlow)
                return true;

            if (!string.IsNullOrWhiteSpace(request.ContractFlowStatus) &&
                AcceptedStatuses.Contains(request.ContractFlowStatus.Trim()))
                return true;

            return request.ContractLinks.Any(link =>
                !string.IsNullOrWhiteSpace(link.Status) &&
                AcceptedStatuses.Contains(link.Status.Trim()));
        }

        public void EnsureCanExecute(PersonnelChangeRequest request)
        {
            if (request.RequiresContractFlow && !IsContractFlowCompleted(request))
                throw new InvalidOperationException("Contract flow must be accepted, signed, or completed before execution.");
        }

        public async Task CreateContractFlowAsync(PersonnelChangeRequest request, CancellationToken ct)
        {
            if (!request.RequiresContractFlow || request.ContractFlowType == PersonnelChangeContractFlowType.None)
                return;

            if (request.ContractLinks.Any())
                return;

            request.Status = PersonnelChangeStatus.PendingContractFlow;
            request.ContractFlowStatus = "Pending";

            var link = request.ContractFlowType switch
            {
                PersonnelChangeContractFlowType.ContractAddendum => await CreateAddendumFlowAsync(request, ct),
                PersonnelChangeContractFlowType.NewContract => await CreateContractRequestFlowAsync(request, ct),
                PersonnelChangeContractFlowType.ContractRenewal => await CreateContractRequestFlowAsync(request, ct),
                PersonnelChangeContractFlowType.ContractTermination => await CreateContractTerminationFlowAsync(request, ct),
                _ => null
            };

            if (link == null)
                return;

            await _personnelChangeRepo.AddContractLinkAsync(link, ct);
            await _personnelChangeRepo.AddHistoryAsync(new PersonnelChangeHistory
            {
                RequestId = request.Id,
                Action = "ContractFlowRequired",
                OldStatus = PersonnelChangeStatus.PendingContractFlow,
                NewStatus = PersonnelChangeStatus.PendingContractFlow,
                ActorAccountId = request.HRAssignedAccountId ?? request.RequestedByAccountId,
                Note = $"Created {request.ContractFlowType} flow for personnel change request.",
                CreatedAt = DateTime.UtcNow
            }, ct);

            await _mediator.Publish(new ContractFlowRequiredEvent
            {
                PersonnelChangeRequestId = request.Id,
                EmployeeId = GetRequiredEmployeeId(request),
                ContractFlowType = request.ContractFlowType,
                ContractId = link.ContractId,
                ContractRequestId = link.ContractRequestId,
                ContractAddendumId = link.ContractAddendumId
            }, ct);
        }

        public Task MarkContractFlowCompletedAsync(int contractFlowReferenceId, CancellationToken ct)
        {
            return MarkContractFlowCompletedAsync(contractFlowReferenceId, null, ct);
        }

        public async Task MarkContractFlowNegotiatingAsync(int contractId, string? note, CancellationToken ct)
        {
            var requests = await _personnelChangeRepo.GetByContractFlowReferenceAsync(contractId, null, ct);

            foreach (var request in requests)
            {
                if (request.Status != PersonnelChangeStatus.PendingContractFlow &&
                    request.Status != PersonnelChangeStatus.Escalated)
                    continue;

                var oldStatus = request.Status;
                request.ContractFlowStatus = "Negotiating";
                request.Status = PersonnelChangeStatus.ContractNegotiating;
                request.UpdatedAt = DateTime.UtcNow;

                foreach (var link in request.ContractLinks.Where(l =>
                             l.ContractId == contractId || l.ContractRequestId == contractId))
                {
                    link.Status = "Negotiating";
                }

                await _personnelChangeRepo.AddHistoryAsync(new PersonnelChangeHistory
                {
                    RequestId = request.Id,
                    Action = "ContractFlowNegotiating",
                    OldStatus = oldStatus,
                    NewStatus = request.Status,
                    Note = string.IsNullOrWhiteSpace(note)
                        ? $"Contract flow is negotiating: {contractId}."
                        : note.Trim(),
                    CreatedAt = DateTime.UtcNow
                }, ct);

                _personnelChangeRepo.Update(request);
            }

            await _unitOfWork.CommitAsync(ct);
        }

        public async Task MarkContractFlowCompletedAsync(int? contractId, int? contractAddendumId, CancellationToken ct)
        {
            if (!contractId.HasValue && !contractAddendumId.HasValue)
                return;

            var requests = await _personnelChangeRepo.GetByContractFlowReferenceAsync(contractId, contractAddendumId, ct);

            foreach (var request in requests)
            {
                if (request.Status != PersonnelChangeStatus.PendingContractFlow &&
                    request.Status != PersonnelChangeStatus.ContractNegotiating &&
                    request.Status != PersonnelChangeStatus.Escalated)
                    continue;

                var oldStatus = request.Status;
                request.ContractFlowStatus = "Accepted";
                request.Status = request.ChangeType is PersonnelChangeType.SeniorAppointment
                    or PersonnelChangeType.Dismissal
                    or PersonnelChangeType.Promotion
                    or PersonnelChangeType.ConvertToOfficial
                    or PersonnelChangeType.VoluntaryTermination
                    ? PersonnelChangeStatus.ContractAccepted
                    : PersonnelChangeStatus.ReadyToExecute;
                request.UpdatedAt = DateTime.UtcNow;

                if (contractId.HasValue && !request.RelatedContractId.HasValue)
                    request.RelatedContractId = contractId.Value;
                if (contractAddendumId.HasValue && !request.RelatedContractAddendumId.HasValue)
                    request.RelatedContractAddendumId = contractAddendumId.Value;

                foreach (var link in request.ContractLinks.Where(l =>
                             (contractId.HasValue &&
                              (l.ContractId == contractId.Value || l.ContractRequestId == contractId.Value)) ||
                             (contractAddendumId.HasValue && l.ContractAddendumId == contractAddendumId.Value)))
                {
                    link.Status = "Accepted";
                    link.CompletedAt = DateTime.UtcNow;
                }

                await _personnelChangeRepo.AddHistoryAsync(new PersonnelChangeHistory
                {
                    RequestId = request.Id,
                    Action = "ContractFlowCompleted",
                    OldStatus = oldStatus,
                    NewStatus = request.Status,
                    Note = contractAddendumId.HasValue
                        ? $"Contract addendum flow completed: {contractAddendumId.Value}."
                        : $"Contract flow completed: {contractId!.Value}.",
                    CreatedAt = DateTime.UtcNow
                }, ct);

                _personnelChangeRepo.Update(request);
            }

            await _unitOfWork.CommitAsync(ct);
        }

        public async Task MarkContractFlowRejectedAsync(int? contractId, int? contractAddendumId, string? reason, CancellationToken ct)
        {
            if (!contractId.HasValue && !contractAddendumId.HasValue)
                return;

            var requests = await _personnelChangeRepo.GetByContractFlowReferenceAsync(contractId, contractAddendumId, ct);
            var rejectionReason = string.IsNullOrWhiteSpace(reason)
                ? "Contract flow was rejected by Module 3."
                : reason.Trim();

            foreach (var request in requests)
            {
                if (request.Status != PersonnelChangeStatus.PendingContractFlow &&
                    request.Status != PersonnelChangeStatus.ContractNegotiating &&
                    request.Status != PersonnelChangeStatus.Escalated)
                    continue;

                var oldStatus = request.Status;
                request.ContractFlowStatus = "Rejected";
                request.Status = PersonnelChangeStatus.ContractRejected;
                request.RejectedReason = string.IsNullOrWhiteSpace(request.RejectedReason)
                    ? rejectionReason
                    : request.RejectedReason;
                request.UpdatedAt = DateTime.UtcNow;

                if (contractId.HasValue && !request.RelatedContractId.HasValue)
                    request.RelatedContractId = contractId.Value;
                if (contractAddendumId.HasValue && !request.RelatedContractAddendumId.HasValue)
                    request.RelatedContractAddendumId = contractAddendumId.Value;

                foreach (var link in request.ContractLinks.Where(l =>
                             (contractId.HasValue &&
                              (l.ContractId == contractId.Value || l.ContractRequestId == contractId.Value)) ||
                             (contractAddendumId.HasValue && l.ContractAddendumId == contractAddendumId.Value)))
                {
                    link.Status = "Rejected";
                    link.CompletedAt = DateTime.UtcNow;
                }

                await _personnelChangeRepo.AddHistoryAsync(new PersonnelChangeHistory
                {
                    RequestId = request.Id,
                    Action = "ContractFlowRejected",
                    OldStatus = oldStatus,
                    NewStatus = request.Status,
                    Note = rejectionReason,
                    CreatedAt = DateTime.UtcNow
                }, ct);

                _personnelChangeRepo.Update(request);
            }

            await _unitOfWork.CommitAsync(ct);
        }

        private async Task<PersonnelChangeContractLink> CreateContractRequestFlowAsync(PersonnelChangeRequest request, CancellationToken ct)
        {
            var employeeId = GetRequiredEmployeeId(request);
            var contract = new Contract
            {
                EmployeeId = employeeId,
                ContractNumber = $"TEMP-PC-{request.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}",
                Status = ContractStatus.PendingHR,
                StartDate = request.EffectiveDate ?? DateTime.UtcNow.Date,
                BasicSalary = 0m,
                InsuranceSalary = 0m,
                NegotiationNote = $"Created from personnel change request #{request.Id}: {request.Reason}"
            };

            await _contractRepo.AddAsync(contract, ct);
            await _unitOfWork.CommitAsync(ct);

            request.RelatedContractId = contract.Id;
            request.RelatedContractRequestId = contract.Id;

            return new PersonnelChangeContractLink
            {
                PersonnelChangeRequestId = request.Id,
                ContractId = contract.Id,
                ContractRequestId = contract.Id,
                ContractFlowType = request.ContractFlowType,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };
        }

        private async Task<PersonnelChangeContractLink> CreateAddendumFlowAsync(PersonnelChangeRequest request, CancellationToken ct)
        {
            var contract = request.RelatedContractId.HasValue
                ? await _contractRepo.GetByIdAsync(request.RelatedContractId.Value, ct)
                : (await _contractRepo.GetByEmployeeIdAsync(GetRequiredEmployeeId(request), ct))
                    .FirstOrDefault(c => c.Status == ContractStatus.Active);

            if (contract == null)
                throw new InvalidOperationException("No active or related contract was found for contract addendum flow.");

            var addendum = new ContractAddendum
            {
                ContractId = contract.Id,
                AddendumNumber = $"PL-PC-{request.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}",
                EffectiveDate = request.EffectiveDate ?? DateTime.UtcNow.Date,
                Content = $"Personnel change request #{request.Id}: {request.Reason}",
                OtherChangesJson = BuildOtherChangesJson(request),
                Status = AddendumStatus.PendingHR
            };

            await _contractAddendumRepo.AddAsync(addendum, ct);
            await _unitOfWork.CommitAsync(ct);

            request.RelatedContractId = contract.Id;
            request.RelatedContractAddendumId = addendum.Id;

            return new PersonnelChangeContractLink
            {
                PersonnelChangeRequestId = request.Id,
                ContractId = contract.Id,
                ContractAddendumId = addendum.Id,
                ContractFlowType = PersonnelChangeContractFlowType.ContractAddendum,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };
        }

        private async Task<PersonnelChangeContractLink> CreateContractTerminationFlowAsync(PersonnelChangeRequest request, CancellationToken ct)
        {
            var contract = request.RelatedContractId.HasValue
                ? await _contractRepo.GetByIdAsync(request.RelatedContractId.Value, ct)
                : (await _contractRepo.GetByEmployeeIdAsync(GetRequiredEmployeeId(request), ct))
                    .FirstOrDefault(c => c.Status == ContractStatus.Active);

            if (contract == null)
                throw new InvalidOperationException("No active or related contract was found for contract termination flow.");

            request.RelatedContractId = contract.Id;

            return new PersonnelChangeContractLink
            {
                PersonnelChangeRequestId = request.Id,
                ContractId = contract.Id,
                ContractFlowType = PersonnelChangeContractFlowType.ContractTermination,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };
        }

        private static string BuildOtherChangesJson(PersonnelChangeRequest request)
        {
            var payload = new Dictionary<string, object?>();

            if (request.NewDepartmentId.HasValue) payload["NewDepartmentId"] = request.NewDepartmentId.Value;
            if (request.NewPositionId.HasValue) payload["NewPositionId"] = request.NewPositionId.Value;
            if (request.NewManagerId.HasValue) payload["NewManagerId"] = request.NewManagerId.Value;
            if (request.NewJobLevelId.HasValue) payload["NewJobLevelId"] = request.NewJobLevelId.Value;
            if (request.NewEmployeeType.HasValue) payload["NewEmployeeType"] = request.NewEmployeeType.Value.ToString();
            if (!string.IsNullOrWhiteSpace(request.Reason)) payload["Reason"] = request.Reason;

            return JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }

        private static int GetRequiredEmployeeId(PersonnelChangeRequest request)
        {
            return request.EmployeeId
                ?? throw new InvalidOperationException("Contract flow requires a selected employee.");
        }
    }
}

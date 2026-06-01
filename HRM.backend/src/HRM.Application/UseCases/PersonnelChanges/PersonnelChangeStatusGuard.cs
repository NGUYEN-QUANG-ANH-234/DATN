using HRM.backend.src.HRM.Core.Entities.PersonnelChanges;
using HRM.backend.src.HRM.Core.Enums;

namespace HRM.backend.src.HRM.Application.UseCases.PersonnelChanges
{
    public static class PersonnelChangeStatusGuard
    {
        public const string SlaEscalatedAction = "PersonnelChangeSlaEscalated";

        public static bool IsAllowed(PersonnelChangeRequest request, params PersonnelChangeStatus[] allowedStatuses)
        {
            if (allowedStatuses.Contains(request.Status))
                return true;

            if (request.Status != PersonnelChangeStatus.Escalated)
                return false;

            var escalatedFrom = request.Histories
                .Where(history => history.Action == SlaEscalatedAction &&
                                  history.NewStatus == PersonnelChangeStatus.Escalated)
                .OrderByDescending(history => history.CreatedAt)
                .FirstOrDefault()
                ?.OldStatus;

            return escalatedFrom.HasValue && allowedStatuses.Contains(escalatedFrom.Value);
        }

        public static string DescribeAllowed(IEnumerable<PersonnelChangeStatus> allowedStatuses)
        {
            return string.Join(", ", allowedStatuses);
        }
    }
}

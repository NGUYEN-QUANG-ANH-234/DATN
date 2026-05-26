namespace HRM.backend.src.HRM.Application.Interfaces.System.Services
{
    public interface INotificationTemplateRenderer
    {
        Task<(string Subject, string BodyHtml)> RenderAsync(
            string templateKey,
            IDictionary<string, string> tokens,
            CancellationToken ct = default);
    }
}

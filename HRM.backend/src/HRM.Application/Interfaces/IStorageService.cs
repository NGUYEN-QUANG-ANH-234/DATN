namespace HRM.backend.src.HRM.Application.Interfaces
{
    public interface IStorageService
    {
        Task<string> UploadFileAsync(IFormFile file, string folderName, CancellationToken ct = default);
    }
}

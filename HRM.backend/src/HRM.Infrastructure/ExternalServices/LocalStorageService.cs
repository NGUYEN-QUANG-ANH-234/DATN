using HRM.backend.src.HRM.Application.Interfaces;

namespace HRM.backend.src.HRM.Infrastructure.ExternalServices
{
    public class LocalStorageService : IStorageService
    {
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".csv", ".jpg", ".jpeg", ".png", ".webp"
        };

        private const long MaxFileSizeBytes = 10 * 1024 * 1024;
        private readonly IWebHostEnvironment _env;

        // Tiêm IWebHostEnvironment để lấy đường dẫn chuẩn xác tới thư mục wwwroot
        public LocalStorageService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string> UploadFileAsync(IFormFile file, string folderName, CancellationToken ct = default)
        {
            if (file == null || file.Length == 0)
                return string.Empty;

            if (file.Length > MaxFileSizeBytes)
                throw new InvalidOperationException("File vượt quá dung lượng cho phép 10MB.");

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
                throw new InvalidOperationException("Định dạng file không được hỗ trợ.");

            // 1. Xác định thư mục lưu trữ (VD: wwwroot/uploads/evidences)
            // Nếu WebRootPath null (khi chạy test), fallback về thư mục hiện tại
            var rootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadFolderPath = Path.Combine(rootPath, "uploads", folderName);

            // Tự động tạo thư mục nếu chưa tồn tại
            if (!Directory.Exists(uploadFolderPath))
            {
                Directory.CreateDirectory(uploadFolderPath);
            }

            // 2. Bảo mật: Không dùng tên file gốc để tránh lỗi ký tự đặc biệt hoặc Path Traversal
            // Chỉ lấy đuôi mở rộng (VD: .jpg, .pdf) và gắn với GUID
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var fullFilePath = Path.Combine(uploadFolderPath, uniqueFileName);

            // 3. Ghi file xuống ổ cứng bất đồng bộ
            using (var stream = new FileStream(fullFilePath, FileMode.Create))
            {
                await file.CopyToAsync(stream, ct);
            }

            // 4. Trả về đường dẫn tương đối để lưu vào DB (dễ dàng gọi từ Frontend)
            // Kết quả VD: "/uploads/evidences/123e4567-e89b-12d3-a456-426614174000.jpg"
            return $"/uploads/{folderName}/{uniqueFileName}";
        }
    }
}

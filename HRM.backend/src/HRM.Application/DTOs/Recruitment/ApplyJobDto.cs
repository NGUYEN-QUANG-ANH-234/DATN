using System.ComponentModel.DataAnnotations;

namespace HRM.backend.src.HRM.Application.DTOs.Recruitment
{
    public class ApplyJobDto
    {
        [Required(ErrorMessage = "Mã tin tuyển dụng là bắt buộc.")]
        public int RecruitmentRequestId { get; set; }

        [Required(ErrorMessage = "Họ và tên là bắt buộc.")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ.")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng đính kèm CV (PDF).")]
        public IFormFile CvFile { get; set; } = null!;
    }
}

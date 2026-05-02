using HRM.backend.src.HRM.Core.Entities.Organization;

namespace HRM.backend.src.HRM.Core.Interfaces.Repositories.Organization
{
    public interface IDepartmentRepository : IBaseRepository<Department>
    {
        // AddAsync(entity) đã được kế thừa sẵn từ IBaseRepository<T>

        // --- Các hàm phục vụ UseCase Giải thể/Biến động Tổ chức ---
        Task DissolveDepartmentAsync(int deptId);
        Task ProcessTerminationAsync(int employeeId);
        Task ProcessTransferAsync(int employeeId, int newDeptId);

        // Hàm hỗ trợ lấy chi tiết phòng ban kèm danh sách nhân sự (để check trước khi giải thể)
        Task<Department?> GetDepartmentWithEmployeesAsync(int deptId);
    }
}

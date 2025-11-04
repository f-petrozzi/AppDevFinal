using EduInsight.Models;

namespace EduInsight.Services;

public interface IEnrollmentRepository
{
    Task<IReadOnlyList<Enrollment>> GetAllAsync();
    Task<Enrollment?> GetByStudentIdAsync(string studentId);
    Task AddAsync(Enrollment enrollment);
    Task UpdateAsync(string originalStudentId, Enrollment updated);
    Task<bool> DeleteAsync(string studentId);
}


using EduInsight.Models;

namespace EduInsight.Services;

public interface IEnrollmentBenchmarkService
{
    Task<BenchmarkSummary> GetBenchmarksAsync(CancellationToken cancellationToken = default);
}


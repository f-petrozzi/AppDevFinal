namespace EduInsight.Models;

public class HomeDashboardViewModel
{
    public int StudentCount { get; set; }
    public double AverageGpa { get; set; }
    public int ProgramCount { get; set; }
    public string TopTerm { get; set; } = "—";
    public int TopTermEnrollmentCount { get; set; }
    public IReadOnlyList<Enrollment> Enrollments { get; set; } = Array.Empty<Enrollment>();
    public BenchmarkSummary? Benchmarks { get; set; }
}

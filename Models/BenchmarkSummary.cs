namespace EduInsight.Models;

public class BenchmarkSummary
{
    public string SourceName { get; init; } = "U.S. Department of Education – College Scorecard";

    public DateTime RetrievedAt { get; init; } = DateTime.UtcNow;

    public int InstitutionCount { get; init; }

    public double AverageEnrollment { get; init; }

    public double MedianEnrollment { get; init; }

    public double? AverageAdmissionRate { get; init; }

    public bool IsEmpty => InstitutionCount == 0;
}

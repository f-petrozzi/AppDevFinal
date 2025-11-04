namespace EduInsight.Models;

public class DataInsightsViewModel
{
    public IReadOnlyList<Enrollment> Enrollments { get; set; } = Array.Empty<Enrollment>();
}

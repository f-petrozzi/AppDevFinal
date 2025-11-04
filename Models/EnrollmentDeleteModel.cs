using System.ComponentModel.DataAnnotations;

namespace EduInsight.Models;

public class EnrollmentDeleteModel
{
    [Required]
    [Display(Name = "Student ID")]
    public string StudentId { get; set; } = string.Empty;

    [Display(Name = "Student name")]
    public string StudentName { get; set; } = string.Empty;

    public string Program { get; set; } = string.Empty;

    public string Term { get; set; } = string.Empty;

    [Display(Name = "GPA")]
    public double Gpa { get; set; }

    [Display(Name = "Enrollment date")]
    [DataType(DataType.Date)]
    public DateTime EnrollmentDate { get; set; }

    [Display(Name = "Reason (optional)")]
    public string? Reason { get; set; }

    public static EnrollmentDeleteModel FromEnrollment(Enrollment enrollment) => new()
    {
        StudentId = enrollment.StudentId,
        StudentName = enrollment.StudentName,
        Program = enrollment.Program,
        Term = enrollment.Term,
        Gpa = enrollment.Gpa,
        EnrollmentDate = enrollment.EnrollmentDate
    };
}

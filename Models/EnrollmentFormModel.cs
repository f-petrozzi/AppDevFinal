using System.ComponentModel.DataAnnotations;

namespace EduInsight.Models;

public class EnrollmentFormModel
{
    [Required]
    [Display(Name = "Student ID")]
    public string StudentId { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Student name")]
    public string StudentName { get; set; } = string.Empty;

    [Required]
    public string Program { get; set; } = string.Empty;

    [Required]
    public string Term { get; set; } = string.Empty;

    [Range(0, 4)]
    [Display(Name = "GPA")]
    public double Gpa { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Enrollment date")]
    public DateTime EnrollmentDate { get; set; } = DateTime.Today;

    public string? OriginalStudentId { get; set; }

    public Enrollment ToEnrollment() => new()
    {
        StudentId = StudentId.Trim(),
        StudentName = StudentName.Trim(),
        Program = Program.Trim(),
        Term = Term.Trim(),
        Gpa = Math.Round(Gpa, 2),
        EnrollmentDate = EnrollmentDate.Date
    };

    public static EnrollmentFormModel FromEnrollment(Enrollment enrollment) => new()
    {
        StudentId = enrollment.StudentId,
        StudentName = enrollment.StudentName,
        Program = enrollment.Program,
        Term = enrollment.Term,
        Gpa = enrollment.Gpa,
        EnrollmentDate = enrollment.EnrollmentDate,
        OriginalStudentId = enrollment.StudentId
    };
}

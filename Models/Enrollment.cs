using System.ComponentModel.DataAnnotations;

namespace EduInsight.Models;

public class Enrollment
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

    [Display(Name = "Enrollment date")]
    public DateTime EnrollmentDate { get; set; }
}

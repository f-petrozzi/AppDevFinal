using EduInsight.Models;
using EduInsight.Services;
using Microsoft.AspNetCore.Mvc;

namespace EduInsight.Controllers;

public class EnrollmentsController : Controller
{
    private readonly IEnrollmentRepository _repository;
    private readonly ILogger<EnrollmentsController> _logger;

    public EnrollmentsController(IEnrollmentRepository repository, ILogger<EnrollmentsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["ActivePage"] = string.Empty;
        ViewData["Title"] = "View enrollments";
        var enrollments = await _repository.GetAllAsync();
        return View(enrollments);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewData["ActivePage"] = string.Empty;
        ViewData["Title"] = "Add enrollment";
        return View(new EnrollmentFormModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EnrollmentFormModel model)
    {
        ViewData["ActivePage"] = string.Empty;
        ViewData["Title"] = "Add enrollment";

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _repository.AddAsync(model.ToEnrollment());
            TempData["StatusMessage"] = $"Enrollment {model.StudentId} added.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.StudentId), ex.Message);
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add enrollment for student {StudentId}", model.StudentId);
            ModelState.AddModelError(string.Empty, "Unable to save the enrollment. Please try again.");
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Update(string? studentId)
    {
        ViewData["ActivePage"] = string.Empty;
        ViewData["Title"] = "Update enrollment";

        if (string.IsNullOrWhiteSpace(studentId))
        {
            // No ID provided: show lookup form so user can enter a Student ID
            return View(new EnrollmentFormModel());
        }

        var enrollment = await _repository.GetByStudentIdAsync(studentId);
        if (enrollment is null)
        {
            TempData["StatusMessage"] = $"Enrollment with Student ID {studentId} was not found.";
            return RedirectToAction(nameof(Index));
        }

        var model = EnrollmentFormModel.FromEnrollment(enrollment);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateLookup(EnrollmentFormModel model)
    {
        ViewData["ActivePage"] = string.Empty;
        ViewData["Title"] = "Update enrollment";

        if (string.IsNullOrWhiteSpace(model.StudentId))
        {
            ModelState.AddModelError(nameof(model.StudentId), "Student ID is required to load the record.");
            return View("Update", model);
        }

        var enrollment = await _repository.GetByStudentIdAsync(model.StudentId);
        if (enrollment is null)
        {
            ModelState.AddModelError(nameof(model.StudentId), $"Enrollment with Student ID {model.StudentId} was not found.");
            return View("Update", model);
        }

        var filled = EnrollmentFormModel.FromEnrollment(enrollment);
        return View("Update", filled);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(EnrollmentFormModel model)
    {
        ViewData["ActivePage"] = string.Empty;
        ViewData["Title"] = "Update enrollment";

        if (string.IsNullOrWhiteSpace(model.OriginalStudentId))
        {
            ModelState.AddModelError(string.Empty, "Original Student ID is required.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _repository.UpdateAsync(model.OriginalStudentId!, model.ToEnrollment());
            TempData["StatusMessage"] = $"Enrollment {model.StudentId} updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (KeyNotFoundException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(model.StudentId), ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update enrollment for student {StudentId}", model.StudentId);
            ModelState.AddModelError(string.Empty, "Unable to update the enrollment. Please try again.");
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(string? studentId)
    {
        ViewData["ActivePage"] = string.Empty;
        ViewData["Title"] = "Delete enrollment";

        if (string.IsNullOrWhiteSpace(studentId))
        {
            // No ID provided: show lookup form with empty model
            return View(new EnrollmentDeleteModel());
        }

        var enrollment = await _repository.GetByStudentIdAsync(studentId);
        if (enrollment is null)
        {
            TempData["StatusMessage"] = $"Enrollment with Student ID {studentId} was not found.";
            return RedirectToAction(nameof(Index));
        }

        return View(EnrollmentDeleteModel.FromEnrollment(enrollment));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLookup(EnrollmentDeleteModel model)
    {
        ViewData["ActivePage"] = string.Empty;
        ViewData["Title"] = "Delete enrollment";

        if (string.IsNullOrWhiteSpace(model.StudentId))
        {
            ModelState.AddModelError(nameof(model.StudentId), "Student ID is required.");
            return View("Delete", model);
        }

        var enrollment = await _repository.GetByStudentIdAsync(model.StudentId);
        if (enrollment is null)
        {
            ModelState.AddModelError(nameof(model.StudentId), $"Enrollment with Student ID {model.StudentId} was not found.");
            return View("Delete", model);
        }

        var filled = EnrollmentDeleteModel.FromEnrollment(enrollment);
        return View("Delete", filled);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(EnrollmentDeleteModel model)
    {
        ViewData["ActivePage"] = string.Empty;
        ViewData["Title"] = "Delete enrollment";

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var removed = await _repository.DeleteAsync(model.StudentId);
            if (!removed)
            {
                ModelState.AddModelError(string.Empty, $"Enrollment with Student ID {model.StudentId} was not found.");
                return View(model);
            }

            TempData["StatusMessage"] = $"Enrollment {model.StudentId} deleted.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete enrollment for student {StudentId}", model.StudentId);
            ModelState.AddModelError(string.Empty, "Unable to delete the enrollment. Please try again.");
            return View(model);
        }
    }
}

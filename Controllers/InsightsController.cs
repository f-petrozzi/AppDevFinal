using EduInsight.Models;
using EduInsight.Services;
using Microsoft.AspNetCore.Mvc;

namespace EduInsight.Controllers;

public class InsightsController : Controller
{
    private readonly IEnrollmentRepository _repository;

    public InsightsController(IEnrollmentRepository repository)
    {
        _repository = repository;
    }

    public async Task<IActionResult> Data()
    {
        ViewData["ActivePage"] = "Data";
        ViewData["Title"] = "Data visualization";

        var enrollments = await _repository.GetAllAsync();
        var model = new DataInsightsViewModel { Enrollments = enrollments };
        return View(model);
    }
}

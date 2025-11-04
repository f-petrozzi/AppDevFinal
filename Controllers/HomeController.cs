using System.Diagnostics;
using EduInsight.Models;
using EduInsight.Services;
using Microsoft.AspNetCore.Mvc;

namespace EduInsight.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IEnrollmentRepository _repository;
    private readonly IEnrollmentBenchmarkService _benchmarkService;

    public HomeController(ILogger<HomeController> logger, IEnrollmentRepository repository, IEnrollmentBenchmarkService benchmarkService)
    {
        _logger = logger;
        _repository = repository;
        _benchmarkService = benchmarkService;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["ActivePage"] = "Home";
        ViewData["Title"] = "Home";

        var enrollmentsTask = _repository.GetAllAsync();
        var benchmarkTask = _benchmarkService.GetBenchmarksAsync();

        await Task.WhenAll(enrollmentsTask, benchmarkTask);

        var enrollments = enrollmentsTask.Result;
        var benchmark = benchmarkTask.Result;

        var studentCount = enrollments
            .Select(e => e.StudentId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        var programCount = enrollments
            .Select(e => e.Program)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        var gpaByStudent = enrollments
            .GroupBy(e => e.StudentId, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Average(e => e.Gpa))
            .ToList();

        var averageGpa = gpaByStudent.Any() ? Math.Round(gpaByStudent.Average(), 2) : 0;

        var topTerm = enrollments
            .GroupBy(e => e.Term)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();

        var model = new HomeDashboardViewModel
        {
            StudentCount = studentCount,
            ProgramCount = programCount,
            AverageGpa = averageGpa,
            TopTerm = topTerm?.Key ?? "—",
            TopTermEnrollmentCount = topTerm?.Count() ?? 0,
            Enrollments = enrollments,
            Benchmarks = benchmark
        };

        return View(model);
    }

    public IActionResult About()
    {
        ViewData["ActivePage"] = "About";
        ViewData["Title"] = "About";
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

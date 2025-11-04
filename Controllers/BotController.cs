using Microsoft.AspNetCore.Mvc;

namespace EduInsight.Controllers;

public class BotController : Controller
{
    public IActionResult Index()
    {
        ViewData["ActivePage"] = "Bot";
        ViewData["Title"] = "Ask EduBot";
        return View();
    }
}


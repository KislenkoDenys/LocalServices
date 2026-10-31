using Microsoft.AspNetCore.Mvc;

namespace LocalServices.Controllers;

public class NotesController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Нотатки";
        return View();
    }
}


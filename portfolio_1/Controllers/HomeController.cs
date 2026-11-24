using Microsoft.AspNetCore.Mvc;
using portfolio.Data;
using portfolio.Models;

namespace portfolio.Controllers;

public class HomeController : Controller
{
    private readonly CourseRepository _courses;
    private readonly ProjectRepository _projects;

    public HomeController(CourseRepository courses, ProjectRepository projects)
    {
        _courses = courses;
        _projects = projects;
    }

    public async Task<IActionResult> Index()
    {
        var courses = await _courses.GetAllAsync();
        var projects = await _projects.GetAllAsync();
        var groups = courses.Select(c => new CourseGroup
        {
            Course = c,
            Projects = projects.Where(p => p.CourseId == c.CourseId).ToList()
        }).ToList();
        return View(groups);
    }

    public async Task<IActionResult> Details(int id)
    {
        var p = await _projects.GetByIdAsync(id);
        if (p == null) return NotFound();
        return View(p);
    }

    public IActionResult Status(int code) => View(code);

    public IActionResult Error() => View();
}
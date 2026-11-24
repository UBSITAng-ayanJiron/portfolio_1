using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using portfolio.Data;
using portfolio.Models;

namespace portfolio.Controllers;

[Authorize]
public class CoursesController : Controller
{
    private readonly CourseRepository _courses;
    public CoursesController(CourseRepository courses) => _courses = courses;

    public async Task<IActionResult> Index() => View(await _courses.GetAllAsync());

    public IActionResult Create() => View(new Course());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Course c)
    {
        if (!ModelState.IsValid) return View(c);
        try
        {
            await _courses.InsertAsync(c);
            TempData["Ok"] = "Course added.";
            return RedirectToAction(nameof(Index));
        }
        catch (PostgresException ex)
        {
            ModelState.AddModelError("", DbErrors.Explain(ex));
            return View(c);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        var c = await _courses.GetByIdAsync(id);
        if (c == null) return NotFound();
        return View(c);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Course c)
    {
        c.CourseId = id;
        if (!ModelState.IsValid) return View(c);
        try
        {
            if (!await _courses.UpdateAsync(c)) return NotFound();
            TempData["Ok"] = "Course saved.";
            return RedirectToAction(nameof(Index));
        }
        catch (PostgresException ex)
        {
            ModelState.AddModelError("", DbErrors.Explain(ex));
            return View(c);
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            if (!await _courses.DeleteAsync(id)) return NotFound();
            TempData["Ok"] = "Course deleted.";
        }
        catch (PostgresException ex)
        {
            TempData["Error"] = DbErrors.Explain(ex);   // e.g. 23503 when it still has projects
        }
        return RedirectToAction(nameof(Index));
    }
}
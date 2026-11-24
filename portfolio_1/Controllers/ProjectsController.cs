using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Npgsql;
using portfolio.Data;
using portfolio.Models;

namespace portfolio.Controllers;

[Authorize]
public class ProjectsController : Controller
{
    private readonly ProjectRepository _projects;
    private readonly CourseRepository _courses;

    public ProjectsController(ProjectRepository projects, CourseRepository courses)
    {
        _projects = projects;
        _courses = courses;
    }

    public async Task<IActionResult> Index() => View(await _projects.GetAllAsync());

    private async Task LoadCoursesAsync()
    {
        var list = (await _courses.GetAllAsync())
            .Select(c => new { c.CourseId, Label = c.Code + " — " + c.Title });
        ViewBag.Courses = new SelectList(list, "CourseId", "Label");
    }

    private static List<string> ParseUrls(string? raw) =>
        (raw ?? "").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                   .Select(s => s.Trim()).Where(s => s.Length > 0).ToList();

    private void CheckUrls(List<string> urls)
    {
        for (int i = 0; i < urls.Count; i++)
            if (!Regex.IsMatch(urls[i], Patterns.ImageUrl))
                ModelState.AddModelError(nameof(ProjectForm.ImageUrls),
                    $"Image {i + 1} is not a valid URL (it must start with http:// or https://).");
    }

    public async Task<IActionResult> Create()
    {
        await LoadCoursesAsync();
        return View(new ProjectForm());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProjectForm f)
    {
        var urls = ParseUrls(f.ImageUrls);
        CheckUrls(urls);
        if (!ModelState.IsValid) { await LoadCoursesAsync(); return View(f); }
        try
        {
            await _projects.AddWithImagesAsync(f, urls);
            TempData["Ok"] = "Project added.";
            return RedirectToAction(nameof(Index));
        }
        catch (PostgresException ex)
        {
            ModelState.AddModelError("", DbErrors.Explain(ex));
            await LoadCoursesAsync();
            return View(f);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        var p = await _projects.GetByIdAsync(id);
        if (p == null) return NotFound();
        await LoadCoursesAsync();
        return View(new ProjectForm
        {
            ProjectId = p.ProjectId,
            CourseId = p.CourseId,
            Title = p.Title,
            Description = p.Description,
            CompletedOn = p.CompletedOn,
            VideoUrl = p.VideoUrl,
            ImageUrls = string.Join("\n", p.Images.Select(i => i.ImageUrl))
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProjectForm f)
    {
        f.ProjectId = id;
        var urls = ParseUrls(f.ImageUrls);
        CheckUrls(urls);
        if (!ModelState.IsValid) { await LoadCoursesAsync(); return View(f); }
        try
        {
            if (!await _projects.UpdateWithImagesAsync(f, urls)) return NotFound();
            TempData["Ok"] = "Project saved.";
            return RedirectToAction(nameof(Index));
        }
        catch (PostgresException ex)
        {
            ModelState.AddModelError("", DbErrors.Explain(ex));
            await LoadCoursesAsync();
            return View(f);
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            if (!await _projects.DeleteAsync(id)) return NotFound();
            TempData["Ok"] = "Project deleted.";
        }
        catch (PostgresException ex)
        {
            TempData["Error"] = DbErrors.Explain(ex);
        }
        return RedirectToAction(nameof(Index));
    }
}
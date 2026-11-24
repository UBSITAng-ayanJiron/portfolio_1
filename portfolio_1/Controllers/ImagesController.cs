using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using portfolio.Data;
using portfolio.Models;

namespace portfolio.Controllers;

[Authorize]
public class ImagesController : Controller
{
    private readonly ProjectRepository _projects;
    private readonly ProjectImageRepository _images;

    public ImagesController(ProjectRepository projects, ProjectImageRepository images)
    {
        _projects = projects;
        _images = images;
    }

    public async Task<IActionResult> Index(int projectId)
    {
        var p = await _projects.GetByIdAsync(projectId);
        if (p == null) return NotFound();
        return View(p);
    }

    private bool ValidUrl(string? url)
    {
        if (!string.IsNullOrWhiteSpace(url) && Regex.IsMatch(url.Trim(), Patterns.ImageUrl)) return true;
        TempData["Error"] = "That is not a valid image URL (it must start with http:// or https://).";
        return false;
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int projectId, string? imageUrl)
    {
        if (ValidUrl(imageUrl))
        {
            try { await _images.AddAsync(projectId, imageUrl!.Trim()); TempData["Ok"] = "Image added."; }
            catch (PostgresException ex) { TempData["Error"] = DbErrors.Explain(ex); }
        }
        return RedirectToAction(nameof(Index), new { projectId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, int projectId, string? imageUrl)
    {
        if (ValidUrl(imageUrl))
        {
            try
            {
                if (!await _images.UpdateAsync(id, imageUrl!.Trim())) return NotFound();
                TempData["Ok"] = "Image saved.";
            }
            catch (PostgresException ex) { TempData["Error"] = DbErrors.Explain(ex); }
        }
        return RedirectToAction(nameof(Index), new { projectId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int projectId)
    {
        try
        {
            if (!await _images.DeleteAsync(id)) return NotFound();
            TempData["Ok"] = "Image deleted.";
        }
        catch (PostgresException ex) { TempData["Error"] = DbErrors.Explain(ex); }
        return RedirectToAction(nameof(Index), new { projectId });
    }
}
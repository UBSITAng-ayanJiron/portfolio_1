using System.ComponentModel.DataAnnotations;

namespace portfolio.Models;

public static class Patterns
{
    public const string ImageUrl = @"^https?://[A-Za-z0-9.\-]+\.[A-Za-z]{2,}(/\S*)?$";
    public const string Video = @"^(https?://)?(www\.)?(youtube\.com/(watch\?v=|shorts/)|youtu\.be/)[A-Za-z0-9_\-]{11}(\S*)?$";
    public const string CourseCode = @"^[A-Z]{2,8}[0-9]{0,3}$";
}

public class Course
{
    public int CourseId { get; set; }

    [Required(ErrorMessage = "Enter the course code.")]
    [StringLength(15)]
    [RegularExpression(Patterns.CourseCode, ErrorMessage = "Use capital letters then optional digits, like DBMWEB1.")]
    public string Code { get; set; } = "";

    [Required(ErrorMessage = "Enter the course title.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Title must be 3 to 100 characters.")]
    public string Title { get; set; } = "";

    [Required(ErrorMessage = "Enter the term, like 2026-2027, 1st semester.")]
    [StringLength(30, ErrorMessage = "Term can be at most 30 characters.")]
    public string Term { get; set; } = "";
}

public class Project   // used to DISPLAY projects
{
    public int ProjectId { get; set; }
    public int CourseId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime CompletedOn { get; set; }
    public string? VideoUrl { get; set; }
    public string CourseCode { get; set; } = "";
    public string CourseTitle { get; set; } = "";
    public string CourseTerm { get; set; } = "";
    public int ImageCount { get; set; }
    public string? ThumbUrl { get; set; }
    public List<ProjectImage> Images { get; set; } = new();
}

public class ProjectImage
{
    public int ImageId { get; set; }
    public int ProjectId { get; set; }
    public string ImageUrl { get; set; } = "";
    public int SortOrder { get; set; }
}

public class CourseGroup
{
    public Course Course { get; set; } = new();
    public List<Project> Projects { get; set; } = new();
}

public class ProjectForm   // used by the add/edit form
{
    public int ProjectId { get; set; }

    [Display(Name = "Course")]
    [Range(1, int.MaxValue, ErrorMessage = "Choose a course.")]
    public int CourseId { get; set; }

    [Required(ErrorMessage = "Enter a title.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Title must be 2 to 100 characters.")]
    public string Title { get; set; } = "";

    [Required(ErrorMessage = "Enter a description.")]
    [StringLength(500, MinimumLength = 10, ErrorMessage = "Description must be 10 to 500 characters.")]
    public string Description { get; set; } = "";

    [DataType(DataType.Date)]
    [Display(Name = "Completed on")]
    public DateTime CompletedOn { get; set; } = DateTime.Today;

    [Display(Name = "YouTube link (optional)")]
    [StringLength(300)]
    [RegularExpression(Patterns.Video, ErrorMessage = "Paste a YouTube link, like https://youtu.be/xxxxxxxxxxx.")]
    public string? VideoUrl { get; set; }

    [Display(Name = "Image URLs (one per line)")]
    public string? ImageUrls { get; set; }
}
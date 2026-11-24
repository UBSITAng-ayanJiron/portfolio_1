using Npgsql;

namespace portfolio.Data;

public static class DbErrors
{
    public static string Explain(PostgresException ex)
    {
        switch (ex.SqlState)
        {
            case PostgresErrorCodes.ForeignKeyViolation: // 23503
                return "This record is linked to other records, so the database refused it. A course that still has projects cannot be deleted, and a project needs an existing course.";
            case PostgresErrorCodes.UniqueViolation:     // 23505
                return ex.ConstraintName switch
                {
                    "uq_course_code" => "That course code already exists.",
                    "uq_project_course_title" => "That course already has a project with this title.",
                    "uq_image_project_url" => "The same image URL appears more than once for this project. Nothing was saved.",
                    _ => "That value already exists."
                };
            case PostgresErrorCodes.CheckViolation:      // 23514
                return ex.ConstraintName switch
                {
                    "ck_course_code_upper" => "The course code must be in capital letters.",
                    "ck_course_title_len" => "The course title is too short.",
                    "ck_project_completed" => "The completed date must be on or after January 1, 2000.",
                    "ck_image_url_http" => "An image URL must start with http:// or https://.",
                    _ => "A value is not allowed by the database rules."
                };
            default:
                return "The database could not complete the request. Please try again.";
        }
    }
}
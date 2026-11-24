using Npgsql;
using NpgsqlTypes;
using portfolio.Models;

namespace portfolio.Data;

public class ProjectRepository
{
    private readonly string _cs;
    private readonly ProjectImageRepository _images;

    public ProjectRepository(IConfiguration config, ProjectImageRepository images)
    {
        _cs = config.GetConnectionString("Supabase")!;
        _images = images;
    }

    private const string SelectSql = @"
        SELECT p.project_id, p.course_id, p.title, p.description, p.completed_on, p.video_url,
               c.code, c.title, c.term,
               (SELECT COUNT(*) FROM portfolio.project_image i WHERE i.project_id = p.project_id),
               (SELECT i.image_url FROM portfolio.project_image i WHERE i.project_id = p.project_id
                ORDER BY i.sort_order, i.image_id LIMIT 1)
        FROM portfolio.project p
        JOIN portfolio.course c ON c.course_id = p.course_id ";

    private static Project Map(NpgsqlDataReader r) => new()
    {
        ProjectId = r.GetInt32(0),
        CourseId = r.GetInt32(1),
        Title = r.GetString(2),
        Description = r.GetString(3),
        CompletedOn = r.GetDateTime(4),
        VideoUrl = r.IsDBNull(5) ? null : r.GetString(5),
        CourseCode = r.GetString(6),
        CourseTitle = r.GetString(7),
        CourseTerm = r.GetString(8),
        ImageCount = (int)r.GetInt64(9),
        ThumbUrl = r.IsDBNull(10) ? null : r.GetString(10)
    };

    public async Task<List<Project>> GetAllAsync()
    {
        var list = new List<Project>();
        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(SelectSql + "ORDER BY c.term, c.code, p.title", conn);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(Map(r));
        return list;
    }

    public async Task<Project?> GetByIdAsync(int id)
    {
        Project? p;
        await using (var conn = new NpgsqlConnection(_cs))
        {
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(SelectSql + "WHERE p.project_id = @id", conn);
            cmd.Parameters.AddWithValue("id", id);
            await using var r = await cmd.ExecuteReaderAsync();
            p = await r.ReadAsync() ? Map(r) : null;
        }
        if (p != null) p.Images = await _images.GetByProjectAsync(id);
        return p;
    }

    private static void AddParams(NpgsqlCommand cmd, ProjectForm f)
    {
        cmd.Parameters.AddWithValue("c", f.CourseId);
        cmd.Parameters.AddWithValue("t", f.Title);
        cmd.Parameters.AddWithValue("d", f.Description);
        cmd.Parameters.Add(new NpgsqlParameter("o", NpgsqlDbType.Date) { Value = f.CompletedOn.Date });
        cmd.Parameters.AddWithValue("v", (object?)f.VideoUrl ?? DBNull.Value);
    }

    // ONE transaction: the project and all its images, or nothing
    public async Task<int> AddWithImagesAsync(ProjectForm f, List<string> urls)
    {
        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            int id;
            await using (var cmd = new NpgsqlCommand(
                @"INSERT INTO portfolio.project (course_id, title, description, completed_on, video_url)
                  VALUES (@c, @t, @d, @o, @v) RETURNING project_id", conn, tx))
            {
                AddParams(cmd, f);
                id = (int)(await cmd.ExecuteScalarAsync())!;
            }
            int order = 1;
            foreach (var url in urls)
                await _images.InsertAsync(conn, tx, id, url, order++);
            await tx.CommitAsync();
            return id;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // ONE transaction: update the project and replace its images, or change nothing
    public async Task<bool> UpdateWithImagesAsync(ProjectForm f, List<string> urls)
    {
        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        try
        {
            int rows;
            await using (var cmd = new NpgsqlCommand(
                @"UPDATE portfolio.project SET course_id = @c, title = @t, description = @d,
                         completed_on = @o, video_url = @v WHERE project_id = @id", conn, tx))
            {
                AddParams(cmd, f);
                cmd.Parameters.AddWithValue("id", f.ProjectId);
                rows = await cmd.ExecuteNonQueryAsync();
            }
            if (rows == 0) { await tx.RollbackAsync(); return false; }

            await _images.DeleteByProjectAsync(conn, tx, f.ProjectId);
            int order = 1;
            foreach (var url in urls)
                await _images.InsertAsync(conn, tx, f.ProjectId, url, order++);
            await tx.CommitAsync();
            return true;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("DELETE FROM portfolio.project WHERE project_id = @id", conn);
        cmd.Parameters.AddWithValue("id", id);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }
}
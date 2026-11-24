using Npgsql;
using portfolio.Models;

namespace portfolio.Data;

public class ProjectImageRepository
{
    private readonly string _cs;
    public ProjectImageRepository(IConfiguration config) => _cs = config.GetConnectionString("Supabase")!;

    public async Task<List<ProjectImage>> GetByProjectAsync(int projectId)
    {
        var list = new List<ProjectImage>();
        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            @"SELECT image_id, project_id, image_url, sort_order FROM portfolio.project_image
              WHERE project_id = @p ORDER BY sort_order, image_id", conn);
        cmd.Parameters.AddWithValue("p", projectId);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            list.Add(new ProjectImage
            {
                ImageId = r.GetInt32(0),
                ProjectId = r.GetInt32(1),
                ImageUrl = r.GetString(2),
                SortOrder = r.GetInt32(3)
            });
        return list;
    }

    // used INSIDE a transaction owned by ProjectRepository
    public async Task InsertAsync(NpgsqlConnection conn, NpgsqlTransaction tx, int projectId, string url, int order)
    {
        await using var cmd = new NpgsqlCommand(
            "INSERT INTO portfolio.project_image (project_id, image_url, sort_order) VALUES (@p, @u, @o)", conn, tx);
        cmd.Parameters.AddWithValue("p", projectId);
        cmd.Parameters.AddWithValue("u", url);
        cmd.Parameters.AddWithValue("o", order);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteByProjectAsync(NpgsqlConnection conn, NpgsqlTransaction tx, int projectId)
    {
        await using var cmd = new NpgsqlCommand(
            "DELETE FROM portfolio.project_image WHERE project_id = @p", conn, tx);
        cmd.Parameters.AddWithValue("p", projectId);
        await cmd.ExecuteNonQueryAsync();
    }

    // single-image operations for the Images screen
    public async Task AddAsync(int projectId, string url)
    {
        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            @"INSERT INTO portfolio.project_image (project_id, image_url, sort_order)
              VALUES (@p, @u, (SELECT COALESCE(MAX(sort_order), 0) + 1 FROM portfolio.project_image WHERE project_id = @p))", conn);
        cmd.Parameters.AddWithValue("p", projectId);
        cmd.Parameters.AddWithValue("u", url);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<bool> UpdateAsync(int imageId, string url)
    {
        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "UPDATE portfolio.project_image SET image_url = @u WHERE image_id = @id", conn);
        cmd.Parameters.AddWithValue("u", url);
        cmd.Parameters.AddWithValue("id", imageId);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int imageId)
    {
        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("DELETE FROM portfolio.project_image WHERE image_id = @id", conn);
        cmd.Parameters.AddWithValue("id", imageId);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }
}
using Npgsql;
using portfolio.Models;

namespace portfolio.Data;

public class CourseRepository
{
    private readonly string _cs;
    public CourseRepository(IConfiguration config) => _cs = config.GetConnectionString("Supabase")!;

    private static Course Map(NpgsqlDataReader r) => new()
    {
        CourseId = r.GetInt32(0),
        Code = r.GetString(1),
        Title = r.GetString(2),
        Term = r.GetString(3)
    };

    public async Task<List<Course>> GetAllAsync()
    {
        var list = new List<Course>();
        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "SELECT course_id, code, title, term FROM portfolio.course ORDER BY term, code", conn);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(Map(r));
        return list;
    }

    public async Task<Course?> GetByIdAsync(int id)
    {
        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "SELECT course_id, code, title, term FROM portfolio.course WHERE course_id = @id", conn);
        cmd.Parameters.AddWithValue("id", id);
        await using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? Map(r) : null;
    }

    public async Task InsertAsync(Course c)
    {
        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "INSERT INTO portfolio.course (code, title, term) VALUES (@c, @t, @m)", conn);
        cmd.Parameters.AddWithValue("c", c.Code);
        cmd.Parameters.AddWithValue("t", c.Title);
        cmd.Parameters.AddWithValue("m", c.Term);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<bool> UpdateAsync(Course c)
    {
        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "UPDATE portfolio.course SET code = @c, title = @t, term = @m WHERE course_id = @id", conn);
        cmd.Parameters.AddWithValue("c", c.Code);
        cmd.Parameters.AddWithValue("t", c.Title);
        cmd.Parameters.AddWithValue("m", c.Term);
        cmd.Parameters.AddWithValue("id", c.CourseId);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await using var conn = new NpgsqlConnection(_cs);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("DELETE FROM portfolio.course WHERE course_id = @id", conn);
        cmd.Parameters.AddWithValue("id", id);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }
}
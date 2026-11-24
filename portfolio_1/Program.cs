using Microsoft.AspNetCore.Authentication.Cookies;
using portfolio.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<CourseRepository>();
builder.Services.AddScoped<ProjectImageRepository>();
builder.Services.AddScoped<ProjectRepository>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o => o.LoginPath = "/Account/Login");

var app = builder.Build();

app.UseExceptionHandler("/Home/Error");                       // friendly page for anything unexpected
app.UseStatusCodePagesWithReExecute("/Home/Status", "?code={0}"); // 404 page
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();
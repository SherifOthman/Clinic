using Clinic.Api.Middleware;
using Clinic.Api.Services;
using Clinic.Application;
using Clinic.Application.Abstractions;
using Clinic.Infrastructure;
using Clinic.Infrastructure.Data;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

// Run the API + the built SPA on one port so other PCs on the LAN just browse to it.
builder.WebHost.UseUrls("http://0.0.0.0:5000");

var connectionString = ResolveConnectionString(builder.Configuration, builder.Environment.ContentRootPath);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, connectionString);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
// Minimal Swagger for now; the JWT security definition will be re-added once the
// Microsoft.OpenApi 2.x namespace layout is confirmed for Swashbuckle 10.3.
builder.Services.AddSwaggerGen();

// Permissive CORS: the app is LAN-served; the Vite dev server also needs access during development.
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader()));

var app = builder.Build();

// Apply migrations + seed roles/admin on startup (zero-config first run).
await DbInitializer.InitializeAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "ok", time = DateTime.UtcNow }));

// Serve the built React SPA (single deploy: API + SPA on one port).
var webRoot = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
if (Directory.Exists(webRoot))
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
    app.MapFallback(context =>
    {
        context.Response.ContentType = "text/html";
        return context.Response.SendFileAsync(Path.Combine(webRoot, "index.html"));
    });
}

app.Run();

static string ResolveConnectionString(IConfiguration config, string contentRoot)
{
    var configured = config["ConnectionStrings:DefaultConnection"];
    if (!string.IsNullOrWhiteSpace(configured)) return configured;
    var dataDir = Path.Combine(contentRoot, "data");
    Directory.CreateDirectory(dataDir);
    return $"Data Source={Path.Combine(dataDir, "clinic.db")}";
}

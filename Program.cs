using FileManager.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<IMarkdownService, MarkdownService>();

// Configure data path from environment or use default
var dataPath = Environment.GetEnvironmentVariable("FILE_MANAGER_DATA_PATH") 
    ?? Path.Combine(Directory.GetCurrentDirectory(), "Data");

builder.Services.AddSingleton(new FileManagerConfig { DataPath = dataPath });

var app = builder.Build();

// Ensure data directory exists
Directory.CreateDirectory(dataPath);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=FileManager}/{action=Index}/{*path}");

Console.WriteLine($"📁 File Manager started!");
Console.WriteLine($"📂 Data path: {dataPath}");
Console.WriteLine($"🌐 Open: http://localhost:5000");

app.Run();

// Configuration class
public class FileManagerConfig
{
    public string DataPath { get; set; } = "";
}

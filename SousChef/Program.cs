using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using SousChef.Core.Parsing;
using SousChef.Data;
using SousChef.Endpoints;
using SousChef.Services;

// Create web application builder; the database goes in SousChef:DataPath (relative to the content root).
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
string dataPath = Path.GetFullPath(builder.Configuration["SousChef:DataPath"] ?? "App_Data", builder.Environment.ContentRootPath);
Directory.CreateDirectory(dataPath);
builder.Services.AddDbContext<SousChefDbContext>(options => options.UseSqlite($"Data Source={Path.Combine(dataPath, "souschef.db")}"));
builder.Services.AddSingleton<RecipeCardParser>();
builder.Services.AddScoped<RecipeLibrary>();
builder.Services.Configure<PaperlessOptions>(builder.Configuration.GetSection("Paperless"));
builder.Services.AddHttpClient<PaperlessClient>(client => client.Timeout = TimeSpan.FromMinutes(2));
builder.Services.AddRazorPages();
builder.Services.AddHealthChecks();
builder.Services.Configure<ForwardedHeadersOptions>(options => options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);

// Create the web application, make sure the database exists, and configure.
WebApplication app = builder.Build();
using (IServiceScope scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<SousChefDbContext>().Database.EnsureCreated();
}
app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();
app.MapHealthChecks("/health");
app.MapRecipeFiles();
app.Run();
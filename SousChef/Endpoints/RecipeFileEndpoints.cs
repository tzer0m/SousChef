using Microsoft.EntityFrameworkCore;
using SousChef.Data;

namespace SousChef.Endpoints;

/// <summary>
/// Serves recipe images and original card PDFs.
/// </summary>
public static class RecipeFileEndpoints
{
    /// <summary>
    /// Maps the image and PDF endpoints.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    public static void MapRecipeFiles(this IEndpointRouteBuilder app)
    {
        app.MapGet("/images/{id:int}/{key}", async (int id, string key, SousChefDbContext db, HttpContext context) =>
        {
            RecipeImageRecord? image = await db.Images.AsNoTracking().FirstOrDefaultAsync(i => i.RecipeRecordId == id && i.Key == key);
            if (image == null)
            {
                return Results.NotFound();
            }
            context.Response.Headers.CacheControl = "public, max-age=2592000, immutable";
            return Results.File(image.Data, image.ContentType);
        });
        app.MapGet("/recipes/{id:int}/pdf", async (int id, SousChefDbContext db) =>
        {
            RecipeRecord? record = await db.Recipes.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            return record == null ? Results.NotFound() : Results.File(record.Pdf, "application/pdf", record.SourceFileName);
        });
    }
}
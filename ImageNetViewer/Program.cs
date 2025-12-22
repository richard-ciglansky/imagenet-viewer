using ImageNetData;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ImageNetViewer;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddAuthorization();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        builder.Services.AddScoped<ImageNetDbContext>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ImageNetDbContext>();
            ImageNetDbInitializer.Seed(context);
        }

        app.MapGet("/image-net", (string? nodePath, int? maxDepth, HttpContext httpContext, ImageNetDbContext dbContext) =>
            {
                return GetNodes(httpContext, dbContext, nodePath, maxDepth);
            })
            .WithName("GetImageNetNode");

        app.MapGet("/image-net-search", (string? nodePath, string? searchTerm, HttpContext httpContext, ImageNetDbContext dbContext) =>
            {
                if (searchTerm.IsNullOrEmpty())
                    return Results.BadRequest("Search term cannot be null or empty");

                return Results.Ok(SearchNodes(dbContext, nodePath, searchTerm).ToArray());
            })
            .WithName("SearchImageNet");

        app.Run();
    }

    private static ImageNetNode[] GetNodes(HttpContext httpContext, ImageNetDbContext context, string? nodePath, int? maxDepth)
    {
        return GetNodes(context, nodePath, maxDepth).ToArray();
    }

    private static IQueryable<ImageNetNode> GetNodes(ImageNetDbContext context, string? nodePath, int? maxDepth)
    {
        int nodeDepth = nodePath.IsNullOrEmpty() ? 0 : nodePath.Count(ch => ch == '>');
        maxDepth ??= 1;
        return context.ImageNetNodes.Where(node => node.Name.StartsWith(MakePathPrefix(nodePath)) && nodeDepth < node.Level && node.Level <= nodeDepth + maxDepth);
    }

    private static IQueryable<ImageNetNode> SearchNodes(ImageNetDbContext context, string? nodePath, string searchTerm)
    {
        Console.Out.WriteLine($"SearchNodes(nodePath = {nodePath}, searchTerm = {searchTerm})");

        return context.ImageNetNodes.Where(node => node.Name.StartsWith(MakePathPrefix(nodePath)) && EF.Functions.Like(node.Title, $"%{searchTerm}%"));
    }

    private static string MakePathPrefix(string? nodePath)
        => nodePath is null ? "" : $"{nodePath} > ";
}
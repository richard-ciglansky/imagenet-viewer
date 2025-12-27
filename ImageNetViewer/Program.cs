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
            return GetNodes(dbContext, nodePath, maxDepth, false);
        })
        .WithName("GetImageNetNode");

        app.MapGet("/image-net-tree-data", (string? nodePath, int? maxDepth, ImageNetDbContext dbContext) =>
        {
            var nodes = GetNodes(dbContext, nodePath, maxDepth, true).GetEnumerator();
            if (!nodes.MoveNext())
                return Results.NotFound($"Node '{nodePath}' not found.");

            return Results.Ok(MakeTreeFrom(nodes));
        })
        .WithName("GetImageNetTreeData");

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
        return GetNodes(context, nodePath, maxDepth, false).ToArray();
    }

    private static IQueryable<ImageNetNode> GetNodes(ImageNetDbContext context, string? nodePath, int? maxDepth, bool includeRoot)
    {
        int nodeDepth = nodePath.IsNullOrEmpty() ? 0 : nodePath.Count(ch => ch == '>');
        if (includeRoot)
            nodeDepth--;

        maxDepth ??= 1;
        return context.ImageNetNodes
            .Where(node => node.Name.StartsWith(MakePathPrefix(nodePath, includeRoot)) && nodeDepth < node.Level && node.Level <= nodeDepth + maxDepth)
            .OrderBy(node => node.Id);
    }

    private static ImageNetTreeNode MakeTreeFrom(IEnumerator<ImageNetNode> nodes)
    {
        ImageNetNode currentNode = nodes.Current;
        ImageNetTreeNode current = new ImageNetTreeNode() { Name = nodes.Current.Title, Size = nodes.Current.Size };
        if (nodes.MoveNext())
            current.Children = ReadChildrenFrom(nodes, currentNode.Level + 1);
        return current;
    }

    private static ImageNetTreeNode[] ReadChildrenFrom(IEnumerator<ImageNetNode> nodes, int depth)
    {
        List<ImageNetTreeNode> children = new List<ImageNetTreeNode>();
        ImageNetTreeNode? currentChild = null;
        do
        {
            ImageNetNode childNode = nodes.Current;
            if (childNode is null || childNode.Level < depth)
                break;

            if (childNode.Level > depth)
            {
                currentChild.Children = ReadChildrenFrom(nodes, childNode.Level);
            } else
            {
                currentChild = new ImageNetTreeNode() { Name = childNode.Title, Size = childNode.Size };
                children.Add(currentChild);

                if (!nodes.MoveNext())
                    break;
            }
        } while (true);

        return children.ToArray();
    }

    private static IQueryable<ImageNetNode> SearchNodes(ImageNetDbContext context, string? nodePath, string searchTerm)
    {
        Console.Out.WriteLine($"SearchNodes(nodePath = {nodePath}, searchTerm = {searchTerm})");

        return context.ImageNetNodes
            .Where(node => node.Name.StartsWith(MakePathPrefix(nodePath, false)) && EF.Functions.Like(node.Title, $"%{searchTerm}%"))
            .OrderBy(node => node.Id);
    }

    private static string MakePathPrefix(string? nodePath, bool includeRoot)
        => nodePath is null ? "" : MakePath(nodePath, includeRoot);

    private static string MakePath(string? nodePath, bool includeRoot)
        => includeRoot ? nodePath : $"{nodePath} > ";
}
using ImageNetData;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ImageNetViewer.Middleware;

namespace ImageNetViewer;

public class Program
{
    public const int MaxTreeDepth = 65536;

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddAuthorization();

        // OpenAPI/Swagger for .NET 8 style
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddScoped<ImageNetDbContext>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // Global exception handling
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        // Serve the frontend from wwwroot
        app.UseDefaultFiles();
        app.UseStaticFiles();

        app.UseHttpsRedirection();

        app.UseAuthorization();

        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ImageNetDbContext>();
            ImageNetDbInitializer.Seed(context);
        }

        app.MapGet("/image-net", (string? nodePath, int? maxDepth, ImageNetDbContext dbContext) =>
                Results.Ok(GetNodes(dbContext, nodePath, maxDepth ?? 1, false).ToArray())).WithName("GetImageNetNode");

        app.MapGet("/image-net-tree-data", (string? nodePath, int? maxDepth, ImageNetDbContext dbContext) =>
        {
            var nodes = GetNodes(dbContext, nodePath, maxDepth ?? MaxTreeDepth, true).GetEnumerator();
            if (!nodes.MoveNext())
                return Results.NotFound($"Node '{nodePath}' not found.");

            var tree = MakeTreeFrom(nodes);

            // Serialize as JSON and return as a file attachment
            var json = System.Text.Json.JsonSerializer.Serialize(tree, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);

            // Force browser download with a filename that includes the nodePath (sanitized)
            var fileName = BuildDownloadFileName(nodePath);
            return Results.File(bytes, contentType: "application/json; charset=utf-8", fileDownloadName: fileName);
        })
        .WithName("GetImageNetTreeData");

        app.MapGet("/image-net-search", (string? nodePath, string? searchTerm, ImageNetDbContext dbContext) =>
            {
                if (searchTerm.IsNullOrEmpty())
                    return Results.BadRequest("Search term cannot be null or empty");

                return Results.Ok(SearchNodes(dbContext, nodePath, searchTerm!).ToArray());
            })
            .WithName("SearchImageNet");

        app.Run();
    }

    private static IQueryable<ImageNetNode> GetNodes(ImageNetDbContext context, string? nodePath, int maxDepth, bool includeRoot)
    {
        int nodeDepth = nodePath.IsNullOrEmpty() ? -1 : nodePath!.Count(ch => ch == '>');
        if (includeRoot)
        {
            nodeDepth--;
            maxDepth++;
        }

        if (maxDepth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDepth), "Max depth must be greater than 0.");
        }

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
            ImageNetNode? childNode = nodes.Current;
            if (childNode is null || childNode.Level < depth)
                break;

            if (childNode.Level > depth)
            {
                currentChild!.Children = ReadChildrenFrom(nodes, childNode.Level);
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
        => nodePath.IsNullOrEmpty() ? "" : MakePath(nodePath!, includeRoot);

    private static string MakePath(string nodePath, bool includeRoot)
        => includeRoot ? nodePath : $"{nodePath} > ";

    private static string BuildDownloadFileName(string? nodePath)
    {
        // Default filename when no nodePath provided
        if (string.IsNullOrWhiteSpace(nodePath))
            return "imagenet-tree.json";

        // Replace the tree separator and spaces with underscores for readability
        var name = nodePath.Replace(" > ", "__");
        name = name.Replace(' ', '_');

        // Remove/replace invalid filename characters
        var invalid = System.IO.Path.GetInvalidFileNameChars();
        var sb = new System.Text.StringBuilder(name.Length);
        foreach (var ch in name)
        {
            if (invalid.Contains(ch) || ch == '"')
                sb.Append('_');
            else
                sb.Append(ch);
        }

        // Collapse multiple underscores
        var sanitized = System.Text.RegularExpressions.Regex.Replace(sb.ToString(), "_+", "_");

        // Limit length to keep filenames reasonable
        const int maxBaseLen = 192;
        if (sanitized.Length > maxBaseLen)
            sanitized = sanitized.Substring(0, maxBaseLen);

        // Ensure not empty after sanitization
        if (string.IsNullOrWhiteSpace(sanitized) || sanitized == "_")
            sanitized = "node";

        return $"imagenet-tree_{sanitized}.json";
    }
}
using System.Text.Json;

namespace ImageNetData;

public static class ImageNetDbInitializer
{
    private static JsonSerializerOptions serializationOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, AllowTrailingCommas = true,  };

    public static void Seed(ImageNetDbContext context)
    {
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        if (!context.ImageNetNodes.Any())
        {
            using (var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "ImageNetStructure.json")))
            {
                Console.Out.Write("Reading ImageNetDb ...");
                var nodes = JsonSerializer.Deserialize<ImageNetNode[]>(stream, serializationOptions);
                Console.Out.Write("\nSeeding ImageNetDb ...");

                var titleNull = nodes.Where(n => n.Title == null);
                Console.Out.WriteLine($"Found {titleNull.Count()} nodes with null titles.");
                context.ImageNetNodes.AddRange(nodes);
                context.SaveChanges();
                Console.Out.WriteLine("\nDone.");
            }
        }
    }
}
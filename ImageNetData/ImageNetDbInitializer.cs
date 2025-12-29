using System.Text.Json;

namespace ImageNetData;

public static class ImageNetDbInitializer
{
    private static JsonSerializerOptions serializationOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, AllowTrailingCommas = true,  };

    /// <summary>
    /// Seeds the database with ImageNet structure.
    /// </summary>
    /// <param name="context"></param>
    public static void Seed(ImageNetDbContext context)
    {
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        using (var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "ImageNetStructure.json")))
        {
            Console.Out.Write("Reading ImageNetDb ...");
            var nodes = JsonSerializer.Deserialize<ImageNetNode[]>(stream, serializationOptions) ?? Array.Empty<ImageNetNode>();
            Console.Out.Write("\nSeeding ImageNetDb ...");
            context.ImageNetNodes.AddRange(nodes);
            Console.Out.Write("\nCommiting changes ...");
            context.SaveChanges();
            Console.Out.WriteLine("\nDone.");
        }
    }
}
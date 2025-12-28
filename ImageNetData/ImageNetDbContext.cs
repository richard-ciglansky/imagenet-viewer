using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ImageNetData;

public class ImageNetDbContext : DbContext
{
    public DbSet<ImageNetNode> ImageNetNodes { get; set; }

    private readonly IConfiguration? _configuration;

    public ImageNetDbContext() { }

    public ImageNetDbContext(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
            return;

        // Build connection string from configuration (appsettings.json / environment)
        var section = _configuration?.GetSection("Database");

        // Allow a full override via Database:ConnectionString if provided
        var connectionOverride = section?["ConnectionString"];
        string conn;
        bool usedOverride = !string.IsNullOrWhiteSpace(connectionOverride);
        if (usedOverride)
        {
            conn = connectionOverride!;
            Console.Out.WriteLine("[DB] Using SQL Server connection string from Database:ConnectionString (password hidden)");
        }
        else
        {
            string server = section?["Server"] ?? "localhost";
            string port = section?["Port"] ?? "1433";
            string database = section?["Database"] ?? "ImageNet";
            string userId = section?["UserId"] ?? "ImageNetUser";
            string password = section?["Password"] ?? "ImageNet@123!";
            bool trustedConnection = false; bool.TryParse(section?["Trusted_Connection"], out trustedConnection);
            bool trustServerCert = true; bool.TryParse(section?["TrustServerCertificate"], out trustServerCert);

            conn = $"Server={server},{port};Database={database};User ID={userId};Password={password};Trusted_Connection={(trustedConnection ? "True" : "False")};TrustServerCertificate={(trustServerCert ? "True" : "False")};";
            Console.Error.WriteLine($"[DB] Building SQL connection to {server}:{port} / DB={database} (password hidden)");
        }

        optionsBuilder.UseSqlServer(
            conn,
            sql =>
            {
                // Enable transient failure retry for container start races
                sql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(3), errorNumbersToAdd: null);
            }
        );
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ImageNetNode>()
            .Property(n => n.Name)
            .HasMaxLength(512)  // All nodes have Name shorter than 512 chars
            .IsRequired();

        modelBuilder.Entity<ImageNetNode>()
            .Property(n => n.Title)
            .HasMaxLength(192)  // All nodes have Title shorter than 192 chars
            .IsRequired();

        modelBuilder.Entity<ImageNetNode>()
            .Property(n => n.Id)
            .ValueGeneratedNever()  // Value is defined in the incoming data, we don't want DB to create it.
            .IsRequired();

        modelBuilder.Entity<ImageNetNode>()
            .Property(n => n.Size);

        modelBuilder.Entity<ImageNetNode>()
            .Property(n => n.Level);

        modelBuilder.Entity<ImageNetNode>()
            .Property(n => n.ParentId);

        modelBuilder.Entity<ImageNetNode>()
            .HasIndex(n => n.Name)  // we want to be able to query by Name -> for selecting node's children.
            .HasDatabaseName("IX_ImageNetNodes_Name");

        modelBuilder.Entity<ImageNetNode>()
            .HasIndex(n => n.Title) // we want to be able to query by Title -> for searching nodes.
            .HasDatabaseName("IX_ImageNetNodes_Title");
    }
}
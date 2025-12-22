using Microsoft.EntityFrameworkCore;

namespace ImageNetData;

public class ImageNetDbContext : DbContext
{
    public DbSet<ImageNetNode> ImageNetNodes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Server=localhost,1433;Database=ImageNet;User ID=ImageNetUser;Password=ImageNet@123!;Trusted_Connection=False;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ImageNetNode>()
            .Property(n => n.Name)
            .HasMaxLength(512)
            .IsRequired();

        modelBuilder.Entity<ImageNetNode>()
            .Property(n => n.Title)
            .HasMaxLength(192)
            .IsRequired();

        modelBuilder.Entity<ImageNetNode>()
            .Property(n => n.Id)
            .ValueGeneratedNever()
            .IsRequired();

        modelBuilder.Entity<ImageNetNode>()
            .Property(n => n.Size);

        modelBuilder.Entity<ImageNetNode>()
            .Property(n => n.Level);

        modelBuilder.Entity<ImageNetNode>()
            .Property(n => n.ParentId);

        modelBuilder.Entity<ImageNetNode>()
            .HasIndex(n => n.Name)
            .HasDatabaseName("IX_ImageNetNodes_Name");

        modelBuilder.Entity<ImageNetNode>()
            .HasIndex(n => n.Title)
            .HasDatabaseName("IX_ImageNetNodes_Title");
    }
}
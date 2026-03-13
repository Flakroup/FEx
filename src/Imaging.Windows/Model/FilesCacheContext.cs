using Microsoft.EntityFrameworkCore;
using System.IO;

namespace FEx.Imaging.Windows.Model;

// ReSharper disable PartialTypeWithSinglePart
public partial class FilesCacheContext : DbContext
// ReSharper restore PartialTypeWithSinglePart
{
    public virtual DbSet<IndexEntry> IndexEntries { get; set; }

    public FilesCacheContext(DbContextOptions<FilesCacheContext> options)
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseSqlite(
                $"data source={Path.Combine(@"C:\ProgramData\Flakroup\ImageCache", "IndexEF.db")}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "2.2.0-rtm-35687");

        modelBuilder.Entity<IndexEntry>(entity =>
        {
            entity.HasKey(e => e.AbsoluteUri);

            entity.Property(e => e.AbsoluteUri)
                .UsePropertyAccessMode(PropertyAccessMode.Property)
                .ValueGeneratedNever();

            entity.HasIndex(e => e.FilePath);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    // ReSharper disable PartialMethodWithSinglePart
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    // ReSharper restore PartialMethodWithSinglePart
}

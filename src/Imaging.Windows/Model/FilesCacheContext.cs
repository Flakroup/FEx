using Microsoft.EntityFrameworkCore;
using System.IO;

namespace FEx.Imaging.Windows.Model;

// ReSharper disable PartialTypeWithSinglePart
public partial class FilesCacheContext : DbContext
// ReSharper restore PartialTypeWithSinglePart
{
    // EF Core populates the DbSet; non-null by construction
    public virtual DbSet<IndexEntry> IndexEntries { get; set; } = null!;

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

            // Nullable columns, as the migrations created them: an entry is indexed before its file is downloaded, so
            // both stay null until then. The non-nullable CLR type comes from the IIndexEntryBase contract only.
            entity.Property(e => e.CheckSum)
                .IsRequired(false);

            entity.Property(e => e.FilePath)
                .IsRequired(false);

            entity.HasIndex(e => e.FilePath);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    // ReSharper disable PartialMethodWithSinglePart
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    // ReSharper restore PartialMethodWithSinglePart
}
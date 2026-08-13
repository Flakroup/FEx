using FEx.OneDrv.Models;
using Microsoft.Graph.Models;
using Shouldly;
using System;
using Xunit;

namespace FEx.OneDrv.Tests;

public sealed class DriveItemMapperTests
{
    [Fact]
    public void MapFile_PopulatesAllFields_WhenFileMetadataPresent()
    {
        var modified = DateTimeOffset.UtcNow;

        var item = new DriveItem
        {
            Id = "file-123",
            Name = "photo.jpg",
            Size = 4096,
            LastModifiedDateTime = modified,
            File = new()
            {
                MimeType = "image/jpeg",
                Hashes = new()
                {
                    QuickXorHash = "qxh-abc",
                    Sha256Hash = "sha-def"
                }
            },
            ParentReference = new()
            {
                Path = "/drive/root:/Pictures"
            },
            CreatedBy = new()
            {
                User = new()
                {
                    DisplayName = "Jan Kowalski"
                }
            },
            Image = new()
            {
                Width = 1920,
                Height = 1080
            }
        };

        var mapped = DriveItemMapper.MapFile(item);

        mapped.Id.ShouldBe("file-123");
        mapped.Name.ShouldBe("photo.jpg");
        mapped.Size.ShouldBe(4096L);
        mapped.MimeType.ShouldBe("image/jpeg");
        mapped.QuickXorHash.ShouldBe("qxh-abc");
        mapped.Sha256Hash.ShouldBe("sha-def");
        mapped.LastModified.ShouldBe(modified);
        mapped.Path.ShouldBe("/drive/root:/Pictures");
        mapped.CreatedBy.ShouldBe("Jan Kowalski");
        mapped.ImageWidth.ShouldBe(1920);
        mapped.ImageHeight.ShouldBe(1080);
    }

    [Fact]
    public void MapFile_HandlesMissingOptionalFields()
    {
        var item = new DriveItem
        {
            Id = "file-1",
            Name = "doc.txt",
            Size = 100,
            File = new()
        };

        var mapped = DriveItemMapper.MapFile(item);

        mapped.Id.ShouldBe("file-1");
        mapped.MimeType.ShouldBeNull();
        mapped.QuickXorHash.ShouldBeNull();
        mapped.CreatedBy.ShouldBeNull();
        mapped.ImageWidth.ShouldBeNull();
        mapped.VideoDurationMs.ShouldBeNull();
    }

    [Fact]
    public void MapFile_MapsVideoDurationToMilliseconds()
    {
        var item = new DriveItem
        {
            Id = "vid-1",
            Name = "movie.mp4",
            File = new(),
            Video = new()
            {
                Duration = 12345L
            }
        };

        var mapped = DriveItemMapper.MapFile(item);

        mapped.VideoDurationMs.ShouldBe(12345);
    }

    [Fact]
    public void MapFolder_PopulatesAllFields()
    {
        var item = new DriveItem
        {
            Id = "folder-9",
            Name = "Documents",
            Folder = new()
            {
                ChildCount = 42
            },
            ParentReference = new()
            {
                Path = "/drive/root:"
            }
        };

        var mapped = DriveItemMapper.MapFolder(item);

        mapped.Id.ShouldBe("folder-9");
        mapped.Name.ShouldBe("Documents");
        mapped.ChildCount.ShouldBe(42);
        mapped.Path.ShouldBe("/drive/root:");
    }

    [Fact]
    public void MapFolder_HandlesNullChildCount()
    {
        var item = new DriveItem
        {
            Id = "folder-empty",
            Name = "Empty",
            Folder = new()
        };

        var mapped = DriveItemMapper.MapFolder(item);

        mapped.ChildCount.ShouldBeNull();
    }
}
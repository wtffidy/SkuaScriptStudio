using SkuaScriptStudio.Models;
using SkuaScriptStudio.Services;
using Xunit;

namespace SkuaScriptStudio.Tests;

public sealed class ScriptLibraryServiceTests
{
    [Fact]
    public async Task ImportAsync_HandlesConcurrentDuplicateNames()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        string firstDirectory = Path.Combine(root, "first");
        string secondDirectory = Path.Combine(root, "second");
        string libraryDirectory = Path.Combine(root, "library");
        Directory.CreateDirectory(firstDirectory);
        Directory.CreateDirectory(secondDirectory);
        string firstPath = Path.Combine(firstDirectory, "Farm.cs");
        string secondPath = Path.Combine(secondDirectory, "Farm.cs");
        await File.WriteAllTextAsync(firstPath, "first");
        await File.WriteAllTextAsync(secondPath, "second");

        try
        {
            ScriptLibraryService service = new(libraryDirectory);
            ScriptLibraryEntry[] entries = await Task.WhenAll(
                service.ImportAsync(firstPath),
                service.ImportAsync(secondPath));

            Assert.Equal(2, entries.Select(entry => entry.Name).Distinct(StringComparer.Ordinal).Count());
            Assert.Equal(2, service.GetEntries().Count);
            string firstContent = await File.ReadAllTextAsync(entries[0].FilePath);
            string secondContent = await File.ReadAllTextAsync(entries[1].FilePath);
            Assert.NotEqual(firstContent, secondContent);
            Assert.Contains(new[] { "first", "second" }, content => content == firstContent);
            Assert.Contains(new[] { "first", "second" }, content => content == secondContent);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task ImportAsync_RejectsNonCSharpFiles()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        string sourcePath = Path.Combine(root, "notes.txt");
        await File.WriteAllTextAsync(sourcePath, "not a script");

        try
        {
            ScriptLibraryService service = new(Path.Combine(root, "library"));

            await Assert.ThrowsAsync<ArgumentException>(() => service.ImportAsync(sourcePath));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task ImportAsync_CopiesScriptIntoLibrary()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        string sourceDirectory = Path.Combine(root, "source");
        string libraryDirectory = Path.Combine(root, "library");
        Directory.CreateDirectory(sourceDirectory);
        string sourcePath = Path.Combine(sourceDirectory, "Farm.cs");
        await File.WriteAllTextAsync(sourcePath, "Core.HuntMonster();");

        try
        {
            ScriptLibraryService service = new(libraryDirectory);
            ScriptLibraryEntry entry = await service.ImportAsync(sourcePath);

            Assert.Equal("Farm.cs", entry.Name);
            Assert.Equal("Core.HuntMonster();", await File.ReadAllTextAsync(entry.FilePath));
            Assert.Equal("Core.HuntMonster();", await File.ReadAllTextAsync(sourcePath));
            Assert.Single(service.GetEntries());
            Assert.Single(await service.GetEntriesAsync());
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task ImportAsync_RenamesDuplicateFiles()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        string sourceDirectory = Path.Combine(root, "source");
        string libraryDirectory = Path.Combine(root, "library");
        Directory.CreateDirectory(sourceDirectory);
        string firstPath = Path.Combine(sourceDirectory, "Farm.cs");
        string secondPath = Path.Combine(sourceDirectory, "Other", "Farm.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(secondPath)!);
        await File.WriteAllTextAsync(firstPath, "first");
        await File.WriteAllTextAsync(secondPath, "second");

        try
        {
            ScriptLibraryService service = new(libraryDirectory);
            ScriptLibraryEntry first = await service.ImportAsync(firstPath);
            ScriptLibraryEntry second = await service.ImportAsync(secondPath);

            Assert.Equal("Farm.cs", first.Name);
            Assert.Equal("Farm (2).cs", second.Name);
            Assert.Equal(2, service.GetEntries().Count);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task Remove_DeletesLibraryCopyOnly()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        string sourcePath = Path.Combine(root, "Farm.cs");
        await File.WriteAllTextAsync(sourcePath, "script");

        try
        {
            ScriptLibraryService service = new(Path.Combine(root, "library"));
            ScriptLibraryEntry entry = await service.ImportAsync(sourcePath);

            service.Remove(entry.FilePath);

            Assert.False(File.Exists(entry.FilePath));
            Assert.True(File.Exists(sourcePath));
            Assert.Empty(service.GetEntries());
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void Remove_RejectsFilesOutsideLibrary()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        string outsidePath = Path.Combine(root, "outside.cs");
        File.WriteAllText(outsidePath, "script");

        try
        {
            ScriptLibraryService service = new(Path.Combine(root, "library"));

            Assert.Throws<InvalidOperationException>(() => service.Remove(outsidePath));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}

using SkuaScriptStudio.Models;
using SkuaScriptStudio.Services;
using Xunit;

namespace SkuaScriptStudio.Tests;

public sealed class ScriptFileServiceTests
{
    [Fact]
    public async Task SaveAsync_CreatesAndOverwritesScript()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        string path = Path.Combine(root, "nested", "Farm.cs");
        ScriptDocument document = new() { Source = "first version" };
        ScriptFileService service = new();

        try
        {
            await service.SaveAsync(document, path);
            Assert.Equal("first version", await File.ReadAllTextAsync(path));
            Assert.Equal(path, document.FilePath);
            Assert.False(document.IsDirty);

            document.Source = "second version";
            document.IsDirty = true;
            await service.SaveAsync(document, path);

            Assert.Equal("second version", await File.ReadAllTextAsync(path));
            Assert.False(document.IsDirty);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task SaveAsync_LeavesDocumentDirtyWhenSourceChangesDuringWrite()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        string path = Path.Combine(root, "Farm.cs");
        ScriptDocument document = new() { Source = new string('a', 1024 * 1024), IsDirty = true };
        ScriptFileService service = new();

        try
        {
            Task saveTask = service.SaveAsync(document, path);
            document.Source = "newer edits";
            await saveTask;

            Assert.Equal(new string('a', 1024 * 1024), await File.ReadAllTextAsync(path));
            Assert.Equal("newer edits", document.Source);
            Assert.True(document.IsDirty);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task SaveAsync_RejectsNullDocument()
    {
        ScriptFileService service = new();

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.SaveAsync(null!, "script.cs"));
    }
}

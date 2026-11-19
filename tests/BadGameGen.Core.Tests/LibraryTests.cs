using BadGameGen.App;

namespace BadGameGen.Core.Tests;

public sealed class LibraryTests : IDisposable
{
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("bgg-lib");
    private string Shipped => Path.Combine(_root.FullName, "shipped");
    private string Lib => Path.Combine(_root.FullName, "library");
    public void Dispose() => _root.Delete(true);

    public LibraryTests() => Directory.CreateDirectory(Shipped);

    private void Ship(string file, string content) => File.WriteAllText(Path.Combine(Shipped, file), content);
    private string Read(string file) => File.ReadAllText(Path.Combine(Lib, file));

    [Fact]
    public void FirstRun_CopiesBuiltInsIntoTheLibrary()
    {
        Ship("core.json", "v1");
        var lib = new Library(Shipped, Lib);
        lib.Sync();
        Assert.Equal("v1", Read("core.json"));
        Assert.True(lib.IsBuiltInFile("core.json"));
        Assert.False(lib.State("core.json")!.Edited);
    }

    [Fact]
    public void UneditedFiles_AreUpdated_EditedFilesAreKept()
    {
        Ship("core.json", "v1");
        Ship("horror.json", "h1");
        new Library(Shipped, Lib).Sync();
        File.WriteAllText(Path.Combine(Lib, "horror.json"), "my spooky edits");

        Ship("core.json", "v2");
        Ship("horror.json", "h2");
        var lib = new Library(Shipped, Lib);
        lib.Sync();

        Assert.Equal("v2", Read("core.json"));               // unedited → updated
        Assert.Equal("my spooky edits", Read("horror.json")); // edited → untouched
        var state = lib.State("horror.json")!;
        Assert.True(state.Edited);
        Assert.True(state.UpdateAvailable);

        lib.ResetToBuiltIn("horror.json");
        Assert.Equal("h2", Read("horror.json"));
        Assert.False(lib.State("horror.json")!.Edited);
    }

    [Fact]
    public void LineEndingChanges_DoNotCountAsEdits()
    {
        Ship("core.json", "a\nb\n");
        var lib = new Library(Shipped, Lib);
        lib.Sync();
        File.WriteAllText(Path.Combine(Lib, "core.json"), "a\r\nb\r\n");
        Assert.False(lib.State("core.json")!.Edited);
    }

    [Fact]
    public void DeletedBuiltIns_StayDeletedUntilRestored()
    {
        Ship("core.json", "v1");
        var lib = new Library(Shipped, Lib);
        lib.Sync();
        lib.Remove("core.json");
        new Library(Shipped, Lib).Sync();
        Assert.False(File.Exists(Path.Combine(Lib, "core.json")));
        var again = new Library(Shipped, Lib);
        Assert.Equal(["core.json"], again.RemovedBuiltIns());
        again.RestoreRemoved();
        Assert.Equal("v1", Read("core.json"));
        Assert.Empty(again.RemovedBuiltIns());
    }

    [Fact]
    public void UntrackedExistingFile_IsNeverOverwritten()
    {
        Directory.CreateDirectory(Lib);
        File.WriteAllText(Path.Combine(Lib, "core.json"), "someone's hand-made core");
        Ship("core.json", "v1");
        var lib = new Library(Shipped, Lib);
        lib.Sync();
        Assert.Equal("someone's hand-made core", Read("core.json"));
        Assert.True(lib.State("core.json")!.Edited);
    }

    [Fact]
    public void Fingerprint_ChangesWhenAFileChanges()
    {
        Ship("core.json", "v1");
        new Library(Shipped, Lib).Sync();
        var before = Library.Fingerprint(Lib);
        File.WriteAllText(Path.Combine(Lib, "core.json"), "v1 but longer");
        Assert.NotEqual(before, Library.Fingerprint(Lib));
    }
}

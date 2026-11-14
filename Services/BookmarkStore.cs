using System.Text.Json;
using BookmarkCli.Models;

namespace BookmarkCli.Services;

public class BookmarkStore
{
  private readonly string _filePath;

  private readonly JsonSerializerOptions _jsonOptions = new()
  {
    WriteIndented = true,
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
  };

  private static (
    List<BookmarkFolder>? Siblings,
    BookmarkFolder? Folder
) FindFolderLocation(
    BookmarkData data,
    string folderPath)
  {
    var segments = folderPath
        .Split(
            '/',
            StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries
        );

    if (segments.Length == 0)
      return (null, null);

    var siblings = data.Folders;

    for (var i = 0; i < segments.Length; i++)
    {
      var folder = siblings.FirstOrDefault(f =>
          f.Name.Equals(
              segments[i],
              StringComparison.OrdinalIgnoreCase
          )
      );

      if (folder is null)
        return (null, null);

      if (i == segments.Length - 1)
        return (siblings, folder);

      siblings = folder.Folders;
    }

    return (null, null);
  }

  private static string NormalizeFolderPath(string path)
  {
    return string.Join(
        "/",
        path.Split(
            '/',
            StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries
        )
    );
  }

  public BookmarkStore()
  {
    var appDataPath = Environment.GetFolderPath(
        Environment.SpecialFolder.LocalApplicationData
    );

    var directory = Path.Combine(
        appDataPath,
        "bookmark-cli"
    );

    Directory.CreateDirectory(directory);

    _filePath = Path.Combine(directory, "bookmarks.json");
  }

  public async Task<BookmarkData> GetDataAsync()
  {
    if (!File.Exists(_filePath))
      return new BookmarkData();

    var json = await File.ReadAllTextAsync(_filePath);

    using var document = JsonDocument.Parse(json);

    // Migrate the old flat bookmark array.
    if (document.RootElement.ValueKind == JsonValueKind.Array)
    {
      var oldBookmarks =
          JsonSerializer.Deserialize<List<Bookmark>>(
              json,
              _jsonOptions
          ) ?? [];

      var migratedData = new BookmarkData
      {
        Bookmarks = oldBookmarks
      };

      await SaveAsync(migratedData);

      return migratedData;
    }

    return JsonSerializer.Deserialize<BookmarkData>(
        json,
        _jsonOptions
    ) ?? new BookmarkData();
  }

  public async Task AddBookmarkAsync(
      Bookmark bookmark,
      string? folderPath = null)
  {
    var data = await GetDataAsync();

    if (string.IsNullOrWhiteSpace(folderPath))
    {
      data.Bookmarks.Add(bookmark);
    }
    else
    {
      var folder = FindFolder(data, folderPath);

      if (folder is null)
        throw new InvalidOperationException(
            $"Folder not found: {folderPath}"
        );

      folder.Bookmarks.Add(bookmark);
    }

    await SaveAsync(data);
  }

  // Keeps compatibility with our original AddCommand.
  public Task AddAsync(Bookmark bookmark)
  {
    return AddBookmarkAsync(bookmark);
  }

  public async Task AddFolderAsync(
      string name,
      string? parentPath = null)
  {
    name = name.Trim();

    if (string.IsNullOrWhiteSpace(name))
      throw new ArgumentException(
          "Folder name cannot be empty."
      );

    if (name.Contains('/'))
      throw new ArgumentException("Folder name cannot contain '/'.");

    var data = await GetDataAsync();

    var targetFolders = string.IsNullOrWhiteSpace(parentPath)
        ? data.Folders
        : FindFolder(data, parentPath)?.Folders
          ?? throw new InvalidOperationException(
              $"Parent folder not found: {parentPath}"
          );

    if (targetFolders.Any(
            f => f.Name.Equals(
                name,
                StringComparison.OrdinalIgnoreCase)))
    {
      throw new InvalidOperationException(
          $"Folder already exists: {name}"
      );
    }

    targetFolders.Add(new BookmarkFolder
    {
      Name = name.Trim()
    });

    await SaveAsync(data);
  }

  public async Task SaveAsync(BookmarkData data)
  {
    var json = JsonSerializer.Serialize(
        data,
        _jsonOptions
    );

    var tempPath = _filePath + ".tmp";

    await File.WriteAllTextAsync(tempPath, json);

    File.Move(tempPath, _filePath, overwrite: true);
  }

  public static BookmarkFolder? FindFolder(
      BookmarkData data,
      string folderPath)
  {
    var segments = folderPath
        .Split('/', StringSplitOptions.RemoveEmptyEntries |
                   StringSplitOptions.TrimEntries);

    if (segments.Length == 0)
      return null;

    var currentFolders = data.Folders;
    BookmarkFolder? current = null;

    foreach (var segment in segments)
    {
      current = currentFolders.FirstOrDefault(
          f => f.Name.Equals(
              segment,
              StringComparison.OrdinalIgnoreCase
          )
      );

      if (current is null)
        return null;

      currentFolders = current.Folders;
    }

    return current;
  }


  public async Task<List<string>> GetFolderPathsAsync()
  {
    var data = await GetDataAsync();

    var paths = new List<string>();

    void Traverse(
        List<BookmarkFolder> folders,
        string parentPath)
    {
      foreach (var folder in folders)
      {
        var path = string.IsNullOrEmpty(parentPath)
            ? folder.Name
            : $"{parentPath}/{folder.Name}";

        paths.Add(path);

        Traverse(folder.Folders, path);
      }
    }

    Traverse(data.Folders, "");

    return paths;
  }


  public async Task<List<BookmarkEntry>> GetAllBookmarksAsync()
  {
    var data = await GetDataAsync();

    var entries = new List<BookmarkEntry>();

    void Traverse(
        List<Bookmark> bookmarks,
        List<BookmarkFolder> folders,
        string? parentPath)
    {
      foreach (var bookmark in bookmarks)
      {
        entries.Add(new BookmarkEntry(
            bookmark,
            parentPath
        ));
      }

      foreach (var folder in folders)
      {
        var path = string.IsNullOrEmpty(parentPath)
            ? folder.Name
            : $"{parentPath}/{folder.Name}";

        Traverse(
            folder.Bookmarks,
            folder.Folders,
            path
        );
      }
    }

    Traverse(data.Bookmarks, data.Folders, null);

    return entries;
  }

  public async Task<bool> DeleteBookmarkAsync(Guid id)
  {
    var data = await GetDataAsync();

    bool RemoveFrom(
        List<Bookmark> bookmarks,
        List<BookmarkFolder> folders)
    {
      var bookmark = bookmarks.FirstOrDefault(
          b => b.Id == id
      );

      if (bookmark is not null)
      {
        bookmarks.Remove(bookmark);
        return true;
      }

      foreach (var folder in folders)
      {
        if (RemoveFrom(folder.Bookmarks, folder.Folders))
          return true;
      }

      return false;
    }

    var removed = RemoveFrom(data.Bookmarks, data.Folders);

    if (removed)
      await SaveAsync(data);

    return removed;
  }

  public async Task<bool> UpdateBookmarkAsync(
      Guid id,
      string? title = null,
      string? url = null)
  {
    var data = await GetDataAsync();

    var entry = FindBookmark(data, id);

    if (entry is null)
      return false;

    if (title is not null)
      entry.Title = title;

    if (url is not null)
      entry.Url = url;

    await SaveAsync(data);

    return true;
  }

  public async Task<bool> MoveBookmarkAsync(
      Guid id,
      string? destinationPath)
  {
    var data = await GetDataAsync();

    // Find the bookmark in the same data instance.
    var bookmark = FindBookmark(data, id);

    if (bookmark is null)
      return false;

    // Validate destination before removing anything.
    var destination = string.IsNullOrWhiteSpace(destinationPath)
        ? null
        : FindFolder(data, destinationPath);

    if (!string.IsNullOrWhiteSpace(destinationPath)
        && destination is null)
    {
      throw new InvalidOperationException(
          $"Destination folder not found: {destinationPath}"
      );
    }

    bool RemoveFrom(
        List<Bookmark> bookmarks,
        List<BookmarkFolder> folders)
    {
      var index = bookmarks.FindIndex(b => b.Id == id);

      if (index >= 0)
      {
        bookmarks.RemoveAt(index);
        return true;
      }

      foreach (var folder in folders)
      {
        if (RemoveFrom(folder.Bookmarks, folder.Folders))
          return true;
      }

      return false;
    }

    if (!RemoveFrom(data.Bookmarks, data.Folders))
      return false;

    if (destination is null)
      data.Bookmarks.Add(bookmark);
    else
      destination.Bookmarks.Add(bookmark);

    await SaveAsync(data);

    return true;
  }
  private static Bookmark? FindBookmark(
      BookmarkData data,
      Guid id)
  {
    Bookmark? Search(
        List<Bookmark> bookmarks,
        List<BookmarkFolder> folders)
    {
      var found = bookmarks.FirstOrDefault(b => b.Id == id);

      if (found is not null)
        return found;

      foreach (var folder in folders)
      {
        found = Search(folder.Bookmarks, folder.Folders);

        if (found is not null)
          return found;
      }

      return null;
    }

    return Search(data.Bookmarks, data.Folders);
  }

  public async Task<bool> RenameFolderAsync(
    string folderPath,
    string newName)
  {
    if (string.IsNullOrWhiteSpace(newName))
      throw new ArgumentException("Folder name cannot be empty.");

    newName = newName.Trim();

    if (newName.Contains('/'))
      throw new ArgumentException("Folder name cannot contain '/'.");

    var data = await GetDataAsync();
    var location = FindFolderLocation(data, folderPath);

    if (location.Folder is null || location.Siblings is null)
      return false;

    var duplicate = location.Siblings.Any(f =>
        !ReferenceEquals(f, location.Folder) &&
        f.Name.Equals(newName, StringComparison.OrdinalIgnoreCase));

    if (duplicate)
      throw new InvalidOperationException(
          $"Folder already exists: {newName}"
      );

    location.Folder.Name = newName;

    await SaveAsync(data);
    return true;
  }

  public async Task<bool> DeleteFolderAsync(string folderPath)
  {
    var data = await GetDataAsync();
    var location = FindFolderLocation(data, folderPath);

    if (location.Folder is null || location.Siblings is null)
      return false;

    // Removing the folder also removes all nested folders and bookmarks.
    location.Siblings.Remove(location.Folder);

    await SaveAsync(data);
    return true;
  }

  public async Task<bool> MoveFolderAsync(
      string sourcePath,
      string? destinationParentPath)
  {
    var data = await GetDataAsync();

    var source = FindFolderLocation(data, sourcePath);

    if (source.Folder is null || source.Siblings is null)
      return false;

    // Resolve destination before modifying the source.
    BookmarkFolder? destinationParent = null;

    if (!string.IsNullOrWhiteSpace(destinationParentPath))
    {
      destinationParent = FindFolder(data, destinationParentPath);

      if (destinationParent is null)
        throw new InvalidOperationException(
            $"Destination folder not found: {destinationParentPath}"
        );
    }

    // Prevent moving a folder into itself or one of its descendants.
    if (!string.IsNullOrWhiteSpace(destinationParentPath))
    {
      var normalizedSource = NormalizeFolderPath(sourcePath);
      var normalizedDestination = NormalizeFolderPath(destinationParentPath);

      if (normalizedDestination.Equals(
              normalizedSource,
              StringComparison.OrdinalIgnoreCase) ||
          normalizedDestination.StartsWith(
              normalizedSource + "/",
              StringComparison.OrdinalIgnoreCase))
      {
        throw new InvalidOperationException(
            "Cannot move a folder into itself or one of its descendants."
        );
      }
    }

    var destinationFolders = destinationParent?.Folders ?? data.Folders;

    // Moving to the same parent does not require any changes.
    if (ReferenceEquals(source.Siblings, destinationFolders))
      return true;

    if (destinationFolders.Any(f =>
        f.Name.Equals(
            source.Folder.Name,
            StringComparison.OrdinalIgnoreCase)))
    {
      throw new InvalidOperationException(
          $"A folder named '{source.Folder.Name}' already exists there."
      );
    }

    source.Siblings.Remove(source.Folder);
    destinationFolders.Add(source.Folder);

    await SaveAsync(data);
    return true;
  }

}

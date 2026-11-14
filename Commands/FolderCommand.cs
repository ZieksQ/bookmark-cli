using BookmarkCli.Services;

namespace BookmarkCli.Commands;

public static class FolderCommand
{
  public static async Task ExecuteAsync(
      string[] args,
      BookmarkStore store)
  {
    if (args.Length == 0)
    {
      await ShowHelpAsync();
      return;
    }

    switch (args[0].ToLowerInvariant())
    {
      case "add":
        if (args.Length == 1)
          await InteractiveAddFolderAsync(store);
        else
          await ManualAddFolderAsync(args[1..], store);
        break;

      case "list":
        await ListFoldersAsync(store);
        break;

      case "rename":
        await RenameAsync(args[1..], store);
        break;

      case "delete":
        await DeleteAsync(args[1..], store);
        break;

      case "move":
        await MoveAsync(args[1..], store);
        break;

      default:
        await Gum.LogAsync(
            "error",
            $"Unknown folder command: {args[0]}"
        );
        await ShowHelpAsync();
        break;
    }
  }

  private static async Task InteractiveAddFolderAsync(
      BookmarkStore store)
  {
    await Gum.WriteAsync("Create Folder", "212", true);

    var name = await Gum.InputAsync(
        "e.g. Development",
        "Folder name> "
    );

    if (string.IsNullOrWhiteSpace(name))
    {
      await Gum.LogAsync("warn", "Folder creation cancelled.");
      return;
    }

    var folders = await store.GetFolderPathsAsync();

    var choices = new[] { "Root" }
        .Concat(folders)
        .ToArray();

    var parent = await Gum.ChooseAsync(
        "Choose parent folder",
        choices
    );

    if (parent is null)
    {
      await Gum.LogAsync("warn", "Folder creation cancelled.");
      return;
    }

    await CreateFolderAsync(
        name,
        parent == "Root" ? null : parent,
        store
    );
  }

  private static async Task ManualAddFolderAsync(
      string[] args,
      BookmarkStore store)
  {
    if (args.Length != 1 &&
        (args.Length != 3 || args[1] != "--parent"))
    {
      await Gum.LogAsync(
          "error",
          "Usage: bookmark folder add <name> [--parent <path>]"
      );
      return;
    }

    var name = args[0];

    var parentPath = args.Length == 3
        ? args[2]
        : null;

    await CreateFolderAsync(name, parentPath, store);
  }

  private static async Task CreateFolderAsync(
      string name,
      string? parentPath,
      BookmarkStore store)
  {
    try
    {
      await store.AddFolderAsync(name, parentPath);

      var location = parentPath ?? "Root";

      await Gum.LogAsync(
          "info",
          $"Folder created: {name} ({location})"
      );
    }
    catch (InvalidOperationException ex)
    {
      await Gum.LogAsync("error", ex.Message);
    }
    catch (ArgumentException ex)
    {
      await Gum.LogAsync("error", ex.Message);
    }
  }

  private static async Task ListFoldersAsync(
      BookmarkStore store)
  {
    var data = await store.GetDataAsync();

    await Gum.WriteAsync("Folders", "212", true);

    if (data.Folders.Count == 0)
    {
      await Gum.LogAsync("info", "No folders found.");
      return;
    }

    foreach (var folder in data.Folders)
    {
      await DisplayFolderAsync(folder, 0);
    }
  }

  private static async Task DisplayFolderAsync(
      Models.BookmarkFolder folder,
      int depth)
  {
    var indent = new string(' ', depth * 2);

    await Gum.WriteAsync(
        $"{indent}📁 {folder.Name}",
        "220",
        true
    );

    foreach (var child in folder.Folders)
    {
      await DisplayFolderAsync(child, depth + 1);
    }
  }

  private static async Task ShowHelpAsync()
  {
    await Gum.WriteAsync("Folder commands", "212", true);

    await Gum.WriteAsync("folder add");
    await Gum.WriteAsync("folder add <name> [--parent <path>]");
    await Gum.WriteAsync("folder list");
  }

  private static async Task RenameAsync(
    string[] args,
    BookmarkStore store)
  {
    string? folderPath;
    string? newName;

    if (args.Length == 0)
    {
      var paths = await store.GetFolderPathsAsync();

      if (paths.Count == 0)
      {
        await Gum.LogAsync("warning", "No folders available.");
        return;
      }

      folderPath = await Gum.ChooseAsync(
          "Select folder to rename",
          paths.ToArray()
      );

      if (folderPath is null)
        return;

      newName = await Gum.InputAsync(
          "New folder name",
          "Name: "
      );
    }
    else if (args.Length == 2)
    {
      folderPath = args[0];
      newName = args[1];
    }
    else
    {
      await Gum.LogAsync(
          "error",
          "Usage: bookmark folder rename <path> <new-name>"
      );
      return;
    }

    if (string.IsNullOrWhiteSpace(newName))
      return;

    try
    {
      var renamed = await store.RenameFolderAsync(
          folderPath,
          newName
      );

      if (renamed)
        await Gum.LogAsync("info", $"Renamed folder to '{newName}'.");
      else
        await Gum.LogAsync("error", "Folder not found.");
    }
    catch (Exception ex) when (
        ex is ArgumentException ||
        ex is InvalidOperationException)
    {
      await Gum.LogAsync("error", ex.Message);
    }
  }

  private static async Task DeleteAsync(
    string[] args,
    BookmarkStore store)
  {
    string? folderPath;

    if (args.Length == 0)
    {
      var paths = await store.GetFolderPathsAsync();

      if (paths.Count == 0)
      {
        await Gum.LogAsync("warning", "No folders available.");
        return;
      }

      folderPath = await Gum.ChooseAsync(
          "Select folder to delete",
          paths.ToArray()
      );

      if (folderPath is null)
        return;
    }
    else if (args.Length == 1)
    {
      folderPath = args[0];
    }
    else
    {
      await Gum.LogAsync(
          "error",
          "Usage: bookmark folder delete <path>"
      );
      return;
    }

    var confirmed = await Gum.ConfirmAsync(
        $"Delete '{folderPath}' and all its contents?"
    );

    if (!confirmed)
    {
      await Gum.LogAsync("info", "Deletion cancelled.");
      return;
    }

    var deleted = await store.DeleteFolderAsync(folderPath);

    if (deleted)
      await Gum.LogAsync("info", $"Deleted folder '{folderPath}'.");
    else
      await Gum.LogAsync("error", "Folder not found.");
  }

  private static async Task MoveAsync(
      string[] args,
      BookmarkStore store)
  {
    string? sourcePath;
    string? destinationPath;

    var paths = await store.GetFolderPathsAsync();

    if (paths.Count == 0)
    {
      await Gum.LogAsync("warning", "No folders available.");
      return;
    }

    if (args.Length == 0)
    {
      sourcePath = await Gum.ChooseAsync(
          "Select folder to move",
          paths.ToArray()
      );

      if (sourcePath is null)
        return;

      var destinations = new List<string> { "Root" };

      destinations.AddRange(
          paths.Where(path =>
              !path.Equals(sourcePath, StringComparison.OrdinalIgnoreCase) &&
              !path.StartsWith(
                  sourcePath + "/",
                  StringComparison.OrdinalIgnoreCase
              )
          )
      );

      var selectedDestination = await Gum.ChooseAsync(
          "Select destination",
          destinations.ToArray()
      );

      if (selectedDestination is null)
        return;

      destinationPath = selectedDestination == "Root"
          ? null
          : selectedDestination;
    }
    else if (args.Length == 3 &&
             args[1] == "--parent")
    {
      sourcePath = args[0];

      destinationPath = args[2].Equals(
          "root",
          StringComparison.OrdinalIgnoreCase
      )
          ? null
          : args[2];
    }
    else
    {
      await Gum.LogAsync(
          "error",
          "Usage: bookmark folder move <source-path> --parent <destination-path|root>"
      );
      return;
    }

    try
    {
      var moved = await store.MoveFolderAsync(
          sourcePath,
          destinationPath
      );

      if (moved)
        await Gum.LogAsync("info", "Folder moved successfully.");
      else
        await Gum.LogAsync("error", "Source folder not found.");
    }
    catch (InvalidOperationException ex)
    {
      await Gum.LogAsync("error", ex.Message);
    }
  }
}

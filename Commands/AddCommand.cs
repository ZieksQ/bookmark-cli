using BookmarkCli.Models;
using BookmarkCli.Services;

namespace BookmarkCli.Commands;

public static class AddCommand
{
  public static async Task ExecuteAsync(
      string[] args,
      BookmarkStore store)
  {
    // No arguments means interactive mode.
    if (args.Length == 0)
    {
      await InteractiveAddAsync(store);
      return;
    }

    // Arguments mean manual mode.
    await ManualAddAsync(args, store);
  }

  private static async Task InteractiveAddAsync(
      BookmarkStore store)
  {
    await Gum.WriteAsync(
        "Add Bookmark",
        "212",
        true
    );

    var title = await Gum.InputAsync(
        "e.g. GitHub",
        "Title> "
    );

    if (string.IsNullOrWhiteSpace(title))
    {
      await Gum.LogAsync("warn", "Bookmark creation cancelled.");
      return;
    }

    var url = await Gum.InputAsync(
        "https://example.com",
        "URL> "
    );

    if (string.IsNullOrWhiteSpace(url))
    {
      await Gum.LogAsync("warn", "Bookmark creation cancelled.");
      return;
    }

    var folders = await store.GetFolderPathsAsync();

    var choices = new[] { "Root" }
        .Concat(folders)
        .ToArray();

    var selectedFolder = await Gum.ChooseAsync(
        "Choose destination folder",
        choices
    );

    if (selectedFolder is null)
    {
      await Gum.LogAsync("warn", "Bookmark creation cancelled.");
      return;
    }

    var folderPath = selectedFolder == "Root"
        ? null
        : selectedFolder;

    await SaveBookmarkAsync(
        title,
        url,
        folderPath,
        store
    );
  }

  private static async Task ManualAddAsync(
      string[] args,
      BookmarkStore store)
  {
    if (args.Length != 2 &&
        (args.Length != 4 || args[2] != "--folder"))
    {
      await Gum.LogAsync(
          "error",
          "Usage: bookmark add <title> <url> [--folder <path>]"
      );
      return;
    }

    var folderPath = args.Length == 4
        ? args[3]
        : null;

    await SaveBookmarkAsync(
        args[0],
        args[1],
        folderPath,
        store
    );
  }

  private static async Task SaveBookmarkAsync(
      string title,
      string url,
      string? folderPath,
      BookmarkStore store)
  {
    if (string.IsNullOrWhiteSpace(title))
    {
      await Gum.LogAsync("error", "Title cannot be empty.");
      return;
    }

    if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
        || (uri.Scheme != Uri.UriSchemeHttp &&
            uri.Scheme != Uri.UriSchemeHttps))
    {
      await Gum.LogAsync("error", "Invalid website URL.");
      return;
    }

    var bookmark = new Bookmark
    {
      Title = title.Trim(),
      Url = uri.AbsoluteUri
    };

    try
    {
      await store.AddBookmarkAsync(bookmark, folderPath);

      var destination = folderPath ?? "Root";

      await Gum.LogAsync(
          "info",
          $"Bookmark added: {bookmark.Title} ({destination})"
      );
    }
    catch (InvalidOperationException ex)
    {
      await Gum.LogAsync("error", ex.Message);
    }
  }
}

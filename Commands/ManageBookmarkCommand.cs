using BookmarkCli.Models;
using BookmarkCli.Services;

namespace BookmarkCli.Commands;

public static class ManageBookmarkCommand
{
  public static async Task ExecuteAsync(
      string action,
      string[] args,
      BookmarkStore store)
  {
    switch (action.ToLowerInvariant())
    {
      case "delete":
        if (args.Length == 0)
          await InteractiveDeleteAsync(store);
        else
          await ManualDeleteAsync(args, store);
        break;

      case "edit":
        if (args.Length == 0)
          await InteractiveEditAsync(store);
        else
          await ManualEditAsync(args, store);
        break;

      case "move":
        if (args.Length == 0)
          await InteractiveMoveAsync(store);
        else
          await ManualMoveAsync(args, store);
        break;

      default:
        await Gum.LogAsync(
            "error",
            "Unknown bookmark action."
        );
        break;
    }
  }

  private static async Task<BookmarkEntry?> SelectBookmarkAsync(
      BookmarkStore store)
  {
    var entries = await store.GetAllBookmarksAsync();

    if (entries.Count == 0)
    {
      await Gum.LogAsync("info", "No bookmarks found.");
      return null;
    }

    var choices = entries.ToDictionary(
        e => $"{e.Bookmark.Title} [{e.Bookmark.Id.ToString()[..8]}] - {e.FolderPath ?? "Root"}",
        e => e
    );

    var selected = await Gum.ChooseAsync(
        "Select a bookmark",
        choices.Keys.ToArray()
    );

    if (selected is null)
      return null;

    return choices.GetValueOrDefault(selected);
  }

  private static async Task InteractiveDeleteAsync(
      BookmarkStore store)
  {
    var entry = await SelectBookmarkAsync(store);

    if (entry is null)
      return;

    var confirmed = await Gum.ConfirmAsync(
        $"Delete '{entry.Bookmark.Title}'?"
    );

    if (!confirmed)
    {
      await Gum.LogAsync("info", "Deletion cancelled.");
      return;
    }

    var deleted = await store.DeleteBookmarkAsync(
        entry.Bookmark.Id
    );

    await Gum.LogAsync(
        deleted ? "info" : "error",
        deleted
            ? $"Bookmark deleted: {entry.Bookmark.Title}"
            : "Bookmark could not be found."
    );
  }

  private static async Task ManualDeleteAsync(
      string[] args,
      BookmarkStore store)
  {
    if (!Guid.TryParse(args[0], out var id))
    {
      await Gum.LogAsync("error", "Invalid bookmark ID.");
      return;
    }

    var confirmed = await Gum.ConfirmAsync(
        $"Delete bookmark {id}?"
    );

    if (!confirmed)
      return;

    var deleted = await store.DeleteBookmarkAsync(id);

    await Gum.LogAsync(
        deleted ? "info" : "error",
        deleted ? "Bookmark deleted." : "Bookmark not found."
    );
  }

  private static async Task InteractiveEditAsync(
      BookmarkStore store)
  {
    var entry = await SelectBookmarkAsync(store);

    if (entry is null)
      return;

    await Gum.WriteAsync(
        $"Editing: {entry.Bookmark.Title}",
        "212",
        true
    );

    await Gum.WriteAsync(
        "Leave a field empty to keep its current value."
    );

    var title = await Gum.InputAsync(
        entry.Bookmark.Title,
        "New title> "
    );

    if (title is null)
      return;

    var url = await Gum.InputAsync(
        entry.Bookmark.Url,
        "New URL> "
    );

    if (url is null)
      return;

    title = string.IsNullOrWhiteSpace(title)
        ? null
        : title.Trim();

    url = string.IsNullOrWhiteSpace(url)
        ? null
        : url.Trim();

    await UpdateAsync(
        entry.Bookmark.Id,
        title,
        url,
        store
    );
  }

  private static async Task ManualEditAsync(
      string[] args,
      BookmarkStore store)
  {
    if (!Guid.TryParse(args[0], out var id))
    {
      await Gum.LogAsync("error", "Invalid bookmark ID.");
      return;
    }

    string? title = null;
    string? url = null;

    for (var i = 1; i < args.Length; i++)
    {
      if (i + 1 >= args.Length)
      {
        await Gum.LogAsync("error", "Missing option value.");
        return;
      }

      switch (args[i])
      {
        case "--title":
          title = args[++i];
          break;

        case "--url":
          url = args[++i];
          break;

        default:
          await Gum.LogAsync(
              "error",
              $"Unknown option: {args[i]}"
          );
          return;
      }
    }

    if (title is null && url is null)
    {
      await Gum.LogAsync(
          "error",
          "Provide --title or --url."
      );
      return;
    }

    await UpdateAsync(id, title, url, store);
  }

  private static async Task UpdateAsync(
      Guid id,
      string? title,
      string? url,
      BookmarkStore store)
  {
    if (title is not null &&
        string.IsNullOrWhiteSpace(title))
    {
      await Gum.LogAsync("error", "Title cannot be empty.");
      return;
    }

    if (url is not null &&
        (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
         (uri.Scheme != Uri.UriSchemeHttp &&
          uri.Scheme != Uri.UriSchemeHttps)))
    {
      await Gum.LogAsync("error", "Invalid website URL.");
      return;
    }

    if (url is not null)
      url = new Uri(url).AbsoluteUri;

    var updated = await store.UpdateBookmarkAsync(
        id,
        title?.Trim(),
        url
    );

    await Gum.LogAsync(
        updated ? "info" : "error",
        updated ? "Bookmark updated." : "Bookmark not found."
    );
  }

  private static async Task InteractiveMoveAsync(
      BookmarkStore store)
  {
    var entry = await SelectBookmarkAsync(store);

    if (entry is null)
      return;

    var folders = await store.GetFolderPathsAsync();

    var choices = new[] { "Root" }
        .Concat(folders)
        .Where(path => !string.Equals(
            path,
            entry.FolderPath,
            StringComparison.OrdinalIgnoreCase
        ))
        .ToArray();

    var destination = await Gum.ChooseAsync(
        "Move bookmark to",
        choices
    );

    if (destination is null)
      return;

    await MoveAsync(
        entry.Bookmark.Id,
        destination == "Root" ? null : destination,
        store
    );
  }

  private static async Task ManualMoveAsync(
      string[] args,
      BookmarkStore store)
  {
    if (args.Length != 3 ||
        args[1] != "--folder")
    {
      await Gum.LogAsync(
          "error",
          "Usage: bookmark move <id> --folder <path|root>"
      );
      return;
    }

    if (!Guid.TryParse(args[0], out var id))
    {
      await Gum.LogAsync("error", "Invalid bookmark ID.");
      return;
    }

    var destination = args[2].Equals(
        "root",
        StringComparison.OrdinalIgnoreCase
    ) ? null : args[2];

    await MoveAsync(id, destination, store);
  }

  private static async Task MoveAsync(
      Guid id,
      string? destination,
      BookmarkStore store)
  {
    try
    {
      var moved = await store.MoveBookmarkAsync(
          id,
          destination
      );

      await Gum.LogAsync(
          moved ? "info" : "error",
          moved
              ? $"Bookmark moved to {destination ?? "Root"}."
              : "Bookmark not found."
      );
    }
    catch (InvalidOperationException ex)
    {
      await Gum.LogAsync("error", ex.Message);
    }
  }
}

using BookmarkCli.Services;

namespace BookmarkCli.Commands;

public static class OpenCommand
{
  public static async Task ExecuteAsync(BookmarkStore store)
  {
    var bookmarks = await store.GetAllBookmarksAsync();

    if (bookmarks.Count == 0)
    {
      await Gum.LogAsync("warning", "No bookmarks found.");
      return;
    }

    try
    {
      var selectedId = await Fzf.SelectBookmarkAsync(bookmarks);

      if (selectedId is null)
      {
        await Gum.LogAsync("info", "No bookmark selected.");
        return;
      }

      var selected = bookmarks.FirstOrDefault(
          entry => entry.Bookmark.Id == selectedId.Value
      );

      if (selected is null)
      {
        await Gum.LogAsync("error", "Selected bookmark not found.");
        return;
      }

      BrowserLauncher.Open(selected.Bookmark.Url);

      await Gum.LogAsync(
          "info",
          $"Opening {selected.Bookmark.Title}"
      );
    }
    catch (Exception ex)
    {
      await Gum.LogAsync("error", ex.Message);
    }
  }
}

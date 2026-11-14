using BookmarkCli.Models;
using BookmarkCli.Services;

namespace BookmarkCli.Commands;

public static class ListCommand
{
  public static async Task ExecuteAsync(
      BookmarkStore store)
  {
    var data = await store.GetDataAsync();

    await Gum.WriteAsync("Bookmarks", "212", true);

    if (data.Bookmarks.Count == 0 &&
        data.Folders.Count == 0)
    {
      await Gum.LogAsync(
          "info",
          "No bookmarks found."
      );

      return;
    }

    foreach (var bookmark in data.Bookmarks)
    {
      await DisplayBookmarkAsync(bookmark, 0);
    }

    foreach (var folder in data.Folders)
    {
      await DisplayFolderAsync(folder, 0);
    }
  }

  private static async Task DisplayFolderAsync(
      BookmarkFolder folder,
      int depth)
  {
    var indent = new string(' ', depth * 2);

    await Gum.WriteAsync(
        $"{indent}📁 {folder.Name}",
        "220",
        true
    );

    foreach (var bookmark in folder.Bookmarks)
    {
      await DisplayBookmarkAsync(
          bookmark,
          depth + 1
      );
    }

    foreach (var child in folder.Folders)
    {
      await DisplayFolderAsync(
          child,
          depth + 1
      );
    }
  }

  private static async Task DisplayBookmarkAsync(
      Models.Bookmark bookmark,
      int depth)
  {
    var indent = new string(' ', depth * 2);

    await Gum.WriteAsync(
        $"{indent}🔗 {bookmark.Title}",
        "250"
    );

    await Gum.WriteAsync(
        $"{indent}   {bookmark.Url}",
        "245"
    );
  }
}

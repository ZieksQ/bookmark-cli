namespace BookmarkCli.Models;

public class BookmarkData
{
  public List<BookmarkFolder> Folders { get; set; } = [];

  public List<Bookmark> Bookmarks { get; set; } = [];
}

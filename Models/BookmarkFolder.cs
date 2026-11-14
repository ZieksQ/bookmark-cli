namespace BookmarkCli.Models;

public class BookmarkFolder
{
  public Guid Id { get; set; } = Guid.NewGuid();

  public string Name { get; set; } = string.Empty;

  public List<BookmarkFolder> Folders { get; set; } = [];

  public List<Bookmark> Bookmarks { get; set; } = [];
}

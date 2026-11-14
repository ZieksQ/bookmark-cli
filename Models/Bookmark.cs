namespace BookmarkCli.Models;

public class Bookmark
{
  public Guid Id { get; set; } = Guid.NewGuid();

  public string Title { get; set; } = string.Empty;

  public string Url { get; set; } = string.Empty;

  public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

  public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

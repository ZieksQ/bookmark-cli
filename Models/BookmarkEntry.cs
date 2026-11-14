namespace BookmarkCli.Models;

public record BookmarkEntry(
    Bookmark Bookmark,
    string? FolderPath
);

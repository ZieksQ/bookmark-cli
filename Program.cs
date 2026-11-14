using BookmarkCli.Commands;
using BookmarkCli.Services;

if (args.Length == 0)
{
  await ShowHelpAsync();
  return;
}

var store = new BookmarkStore();

switch (args[0].ToLowerInvariant())
{
  case "add":
    await AddCommand.ExecuteAsync(args[1..], store);
    break;

  case "list":
    await ListCommand.ExecuteAsync(store);
    break;

  case "folder":
    await FolderCommand.ExecuteAsync(args[1..], store);
    break;

  case "delete":
  case "edit":
  case "move":
    await ManageBookmarkCommand.ExecuteAsync(
        args[0],
        args[1..],
        store
    );
    break;

  case "open":
    await OpenCommand.ExecuteAsync(store);
    break;

  default:
    await Gum.LogAsync(
        "error",
        $"Unknown command: {args[0]}"
    );
    await ShowHelpAsync();
    break;
}

static async Task ShowHelpAsync()
{
  await Gum.WriteAsync("Bookmark CLI", "212", true);

  await Gum.WriteAsync("""
    Usage:
      bookmark add [title] [url] [--folder <path>]
      bookmark list

      bookmark folder add [name] [--parent <path>]
      bookmark folder list

      bookmark delete [id]
      bookmark edit [id] [--title <title>] [--url <url>]
      bookmark move [id] --folder <path|root>

      bookmark folder rename [path] [new-name]
      bookmark folder delete [path]
      bookmark folder move [source-path] --parent <destination-path|root>
    """);
}

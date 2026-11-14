using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using BookmarkCli.Models;

namespace BookmarkCli.Services;

public static class Fzf
{
  public static async Task<Guid?> SelectBookmarkAsync(
      List<BookmarkEntry> entries)
  {
    if (entries.Count == 0)
      return null;

    var tempDirectory = Path.Combine(
        Path.GetTempPath(),
        "bookmark-cli"
    );

    Directory.CreateDirectory(tempDirectory);

    var selectionId = Guid.NewGuid().ToString("N");
    var inputPath = Path.Combine(tempDirectory, $"{selectionId}.txt");
    var outputPath = Path.Combine(tempDirectory, $"{selectionId}.selected");

    try
    {
      var lines = entries.Select(entry =>
      {
        var bookmark = entry.Bookmark;

        var title = CleanDisplayText(bookmark.Title);
        var folder = CleanDisplayText(
                  entry.FolderPath ?? "Root"
              );

        var url = CleanDisplayText(bookmark.Url);

        return $"{bookmark.Id}\t{title}\t{folder}\t{url}";
      });

      await File.WriteAllLinesAsync(
          inputPath,
          lines,
          Encoding.UTF8
      );

      var processInfo = CreateTerminalProcess(
          inputPath,
          outputPath
      );

      using var process = Process.Start(processInfo);

      if (process is null)
        throw new InvalidOperationException(
            "Could not start the terminal."
        );

      await process.WaitForExitAsync();

      if (!File.Exists(outputPath))
        return null;

      var selectedLine = await File.ReadAllTextAsync(outputPath);

      if (string.IsNullOrWhiteSpace(selectedLine))
        return null;

      var selectedId = selectedLine
          .Split('\t', 2)[0];

      return Guid.TryParse(selectedId, out var id)
          ? id
          : null;
    }
    finally
    {
      if (File.Exists(inputPath))
        File.Delete(inputPath);

      if (File.Exists(outputPath))
        File.Delete(outputPath);
    }
  }

  private static ProcessStartInfo CreateTerminalProcess(
      string inputPath,
      string outputPath)
  {
    var previewCommand =
    "printf 'Title: %s\\nFolder: %s\\nURL: %s\\n' {2} {3} {4}";

    var fzfCommand =
        $"fzf " +
        $"--delimiter='\\t' " +
        $"--with-nth=2..3 " +
        $"--layout=reverse " +
        $"--border " +
        $"--border-label=' Bookmarks ' " +
        $"--header='Enter: Open | Esc: Cancel' " +
        $"--preview={ShellQuote(previewCommand)} " +
        $"--preview-window=right:50%:wrap " +
        $"< {ShellQuote(inputPath)} " +
        $"> {ShellQuote(outputPath)}";

    if (OperatingSystem.IsWindows())
    {
      previewCommand =
          "echo Title: {2} & echo Folder: {3} & echo URL: {4}";

      var command =
          $"fzf --delimiter=\\t " +
          $"--with-nth=2..3 " +
          $"--layout=reverse " +
          $"--border " +
          $"--header=\"Enter: Open ^| Esc: Cancel\" " +
          $"--preview=\"{previewCommand}\" " +
          $"--preview-window=right:50%:wrap " +
          $"< \"{inputPath}\" > \"{outputPath}\"";
      return new ProcessStartInfo
      {
        FileName = "wt.exe",
        UseShellExecute = false,
        ArgumentList =
                {
                    "--wait",
                    "new-window",
                    "--title",
                    "Bookmark CLI",
                    "cmd.exe",
                    "/d",
                    "/c",
                    command
                }
      };
    }

    if (OperatingSystem.IsLinux())
      return CreateLinuxTerminal(fzfCommand);

    throw new PlatformNotSupportedException(
        "Bookmark CLI terminal selection currently supports Linux and Windows."
    );
  }

  private static ProcessStartInfo CreateLinuxTerminal(
      string command)
  {
    var terminal = new[]
    {
            "alacritty",
            "kitty",
            "wezterm",
            "foot",
            "gnome-terminal",
            "konsole",
            "xfce4-terminal",
            "xterm"
        }.FirstOrDefault(IsExecutableAvailable);

    if (terminal is null)
    {
      throw new InvalidOperationException(
          "No supported terminal emulator was found. " +
          "Install one or add it to your PATH."
      );
    }

    var startInfo = new ProcessStartInfo
    {
      FileName = terminal,
      UseShellExecute = false
    };

    switch (terminal)
    {
      case "alacritty":
        AddArguments(
            startInfo,
            "--class", "bookmark-cli,bookmark-cli",
            "--title", "Bookmark CLI",
            "-e", "sh", "-lc", command
        );
        break;

      case "kitty":
        AddArguments(
            startInfo,
            "--class", "bookmark-cli",
            "--title", "Bookmark CLI",
            "sh", "-lc", command
        );
        break;

      case "wezterm":
        AddArguments(
            startInfo,
            "start",
            "--class", "bookmark-cli",
            "--",
            "sh", "-lc", command
        );
        break;

      case "foot":
        AddArguments(
            startInfo,
            "--title", "Bookmark CLI",
            "sh", "-lc", command
        );
        break;

      case "gnome-terminal":
        AddArguments(
            startInfo,
            "--title=Bookmark CLI",
            "--",
            "sh", "-lc", command
        );
        break;

      case "konsole":
        AddArguments(
            startInfo,
            "--separate",
            "--title", "Bookmark CLI",
            "-e", "sh", "-lc", command
        );
        break;

      case "xfce4-terminal":
        AddArguments(
            startInfo,
            "--disable-server",
            "--title=Bookmark CLI",
            "--command=sh -lc " + ShellQuote(command)
        );
        break;

      case "xterm":
        AddArguments(
            startInfo,
            "-T", "Bookmark CLI",
            "-e", "sh", "-lc", command
        );
        break;
    }

    return startInfo;
  }

  private static void AddArguments(
      ProcessStartInfo info,
      params string[] arguments)
  {
    foreach (var argument in arguments)
      info.ArgumentList.Add(argument);
  }

  private static bool IsExecutableAvailable(string name)
  {
    var path = Environment.GetEnvironmentVariable("PATH");

    if (string.IsNullOrWhiteSpace(path))
      return false;

    return path
        .Split(Path.PathSeparator)
        .Any(directory =>
            File.Exists(Path.Combine(directory, name))
        );
  }

  private static string ShellQuote(string value)
  {
    return "'" + value.Replace("'", "'\\''") + "'";
  }

  private static string CleanDisplayText(string value)
  {
    return value
        .Replace('\t', ' ')
        .Replace('\r', ' ')
        .Replace('\n', ' ');
  }
}

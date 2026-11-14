using System.ComponentModel;
using System.Diagnostics;

namespace BookmarkCli.Services;

public static class Gum
{
  public static async Task LogAsync(
      string level,
      string message)
  {
    var startInfo = new ProcessStartInfo
    {
      FileName = "gum",
      UseShellExecute = false
    };

    startInfo.ArgumentList.Add("log");
    startInfo.ArgumentList.Add("--level");
    startInfo.ArgumentList.Add(level);
    startInfo.ArgumentList.Add(message);

    try
    {
      using var process = Process.Start(startInfo);

      if (process is null)
        throw new InvalidOperationException("Could not start Gum.");

      await process.WaitForExitAsync();

      if (process.ExitCode != 0)
        Console.WriteLine($"[{level.ToUpper()}] {message}");
    }
    catch (Win32Exception)
    {
      Console.WriteLine($"[{level.ToUpper()}] {message}");
    }
  }

  public static async Task WriteAsync(
      string message,
      string? color = null,
      bool bold = false)
  {
    var startInfo = new ProcessStartInfo
    {
      FileName = "gum",
      UseShellExecute = false,
      RedirectStandardOutput = true
    };

    startInfo.ArgumentList.Add("style");

    if (color is not null)
    {
      startInfo.ArgumentList.Add("--foreground");
      startInfo.ArgumentList.Add(color);
    }

    if (bold)
      startInfo.ArgumentList.Add("--bold");

    startInfo.ArgumentList.Add(message);

    try
    {
      using var process = Process.Start(startInfo);

      if (process is null)
        throw new InvalidOperationException("Could not start Gum.");

      var output = await process.StandardOutput.ReadToEndAsync();
      await process.WaitForExitAsync();

      Console.Write(output);
    }
    catch (Win32Exception)
    {
      Console.WriteLine(message);
    }
  }

  public static async Task<string?> InputAsync(
      string placeholder,
      string prompt)
  {
    var startInfo = new ProcessStartInfo
    {
      FileName = "gum",
      UseShellExecute = false,
      RedirectStandardOutput = true
    };

    startInfo.ArgumentList.Add("input");
    startInfo.ArgumentList.Add("--placeholder");
    startInfo.ArgumentList.Add(placeholder);
    startInfo.ArgumentList.Add("--prompt");
    startInfo.ArgumentList.Add(prompt);

    return await RunInteractiveAsync(startInfo);
  }

  public static async Task<string?> ChooseAsync(
      string title,
      params string[] choices)
  {
    if (choices.Length == 0)
      return null;

    var startInfo = new ProcessStartInfo
    {
      FileName = "gum",
      UseShellExecute = false,
      RedirectStandardOutput = true
    };

    startInfo.ArgumentList.Add("choose");
    startInfo.ArgumentList.Add("--header");
    startInfo.ArgumentList.Add(title);

    foreach (var choice in choices)
      startInfo.ArgumentList.Add(choice);

    return await RunInteractiveAsync(startInfo);
  }

  public static async Task<bool> ConfirmAsync(
      string message)
  {
    var startInfo = new ProcessStartInfo
    {
      FileName = "gum",
      UseShellExecute = false
    };

    startInfo.ArgumentList.Add("confirm");
    startInfo.ArgumentList.Add(message);

    try
    {
      using var process = Process.Start(startInfo);

      if (process is null)
        return false;

      await process.WaitForExitAsync();

      return process.ExitCode == 0;
    }
    catch (Win32Exception)
    {
      Console.WriteLine("Gum is required for this interaction.");
      return false;
    }
  }

  private static async Task<string?> RunInteractiveAsync(
      ProcessStartInfo startInfo)
  {
    try
    {
      using var process = Process.Start(startInfo);

      if (process is null)
        return null;

      var output = await process.StandardOutput.ReadToEndAsync();

      await process.WaitForExitAsync();

      if (process.ExitCode != 0)
        return null;

      return output.TrimEnd('\r', '\n');
    }
    catch (Win32Exception)
    {
      Console.WriteLine("Gum is not installed or unavailable in PATH.");
      return null;
    }
  }
}

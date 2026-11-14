using System.Diagnostics;

namespace BookmarkCli.Services;

public static class BrowserLauncher
{
  public static void Open(string url)
  {
    if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
        || (uri.Scheme != Uri.UriSchemeHttp
            && uri.Scheme != Uri.UriSchemeHttps))
    {
      throw new ArgumentException(
          "Only HTTP and HTTPS URLs can be opened.",
          nameof(url)
      );
    }

    Process.Start(new ProcessStartInfo
    {
      FileName = uri.AbsoluteUri,
      UseShellExecute = true
    });
  }
}

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace VisualStudioSolutionSecrets.Utilities
{
    internal static class WebBrowser
    {
        public static void OpenUrl(Uri uri)
        {
            ArgumentNullException.ThrowIfNull(uri);

            string url = uri.ToString();

            try
            {
                _ = Process.Start(url);
                return;
            }
            catch
            {
                // ignored
            }

            try
            {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&", StringComparison.Ordinal);
                    _ = Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                    return;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    _ = Process.Start("xdg-open", url);
                    return;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    _ = Process.Start("open", url);
                    return;
                }
            }
            catch
            {
                // ignored
            }

            Console.WriteLine($"\nOpen the URL below with your preferred browser:\n{url}\n");
        }
    }
}

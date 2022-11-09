using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace VisualStudioSolutionSecrets.Utilities
{
    public static class WebBrowser
    {
        public static void OpenUrl(Uri uri)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));

            string url = uri.ToString();

            try
            {
                Process.Start(url);
                return;
            }
            catch
            { }

            try
            {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&", StringComparison.Ordinal);
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                    return;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                    return;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                    return;
                }
            }
            catch
            { }

            Console.WriteLine($"\nOpen the URL below with your preferred browser:\n{url}\n");
        }
    }
}

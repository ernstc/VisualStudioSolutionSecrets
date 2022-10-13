using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace VisualStudioSolutionSecrets.Tests.Helpers
{
    public static class FilesHelper
    {
        public static string GetAbsoluteTestPath(string relativePath)
        {
            var assemblyLocation = new Uri(Assembly.GetExecutingAssembly().Location);
            var codeBasePath = Uri.UnescapeDataString(assemblyLocation.AbsolutePath);
            var dirPath = Path.GetDirectoryName(codeBasePath);
            return Path.Combine(dirPath ?? string.Empty, relativePath);
        }
    }
}

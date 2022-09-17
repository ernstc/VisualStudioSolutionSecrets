using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace VisualStudioSolutionSecrets.Tests
{
    public static class Helpers
    {
        public static string GetAbsoluteTestPath(string relativePath)
        {
            var assemblyLocation = new Uri(Assembly.GetExecutingAssembly().Location);
            var codeBasePath = Uri.UnescapeDataString(assemblyLocation.AbsolutePath);
            var dirPath = Path.GetDirectoryName(codeBasePath);
            return Path.Combine(dirPath ?? String.Empty, relativePath);
        }
    }
}

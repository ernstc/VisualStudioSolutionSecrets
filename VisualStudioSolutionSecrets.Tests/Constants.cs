using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisualStudioSolutionSecrets.Tests
{
    public static class Constants
    {
        public static readonly string SampleFilesPath = Helpers.GetAbsoluteTestPath("SampleFiles");
        public static readonly string ConfigFilesPath = Path.Combine(SampleFilesPath, "configFiles");
        public static readonly string SecretFilesPath = Path.Combine(SampleFilesPath, "secrets");
        public static readonly string SolutionFilesPath = Path.Combine(SampleFilesPath, "solutionFiles");
    }
}

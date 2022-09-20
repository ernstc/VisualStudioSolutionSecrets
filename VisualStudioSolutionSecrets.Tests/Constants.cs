using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisualStudioSolutionSecrets.Tests.Helpers;

namespace VisualStudioSolutionSecrets.Tests
{
    public static class Constants
    {
        public static readonly string SampleFilesPath = FilesHelper.GetAbsoluteTestPath("SampleFiles");
        public static readonly string ConfigFilesPath = Path.Combine(SampleFilesPath, "configFiles");
        public static readonly string RepositoryFilesPath = Path.Combine(SampleFilesPath, "repository");
        public static readonly string SecretFilesPath = Path.Combine(SampleFilesPath, "secrets");
        public static readonly string SolutionFilesPath = Path.Combine(SampleFilesPath, "solutionFiles");
        public static readonly string TestFilesPath = Path.Combine(SampleFilesPath, "testFiles");


        public const string PASSPHRASE = "Passphrase.1";
    }
}

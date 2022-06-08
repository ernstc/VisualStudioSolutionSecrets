using System;


namespace VisualStudioSolutionSecrets
{

    internal class HeaderFile
    {
        public string visualStudioSolutionSecretsVersion { get; set; } = null!;
        public DateTime lastUpload { get; set; }
        public string solutionFile { get; set; } = null!;


        public bool IsVersionSupported()
        {
            Version headerVersion = new Version(visualStudioSolutionSecretsVersion);
            Version minVersion = new Version(Versions.MinimumFileFormatSupported);
            if (headerVersion.Major > minVersion.Major)
            {
                Console.WriteLine($"\n    ERR: Header file has incompatible version {visualStudioSolutionSecretsVersion}");
                Console.WriteLine($"\n         Consider to install an updated version of this tool.");
                return false;
            }
            return true;
        }

    }
}

using System;


namespace VisualStudioSolutionSecrets
{

    public class HeaderFile
    {
        public string visualStudioSolutionSecretsVersion { get; set; } = null!;
        public DateTime lastUpload { get; set; }
        public string solutionFile { get; set; } = null!;


        public bool IsVersionSupported()
        {
            try
            {
                Version headerVersion = new Version(visualStudioSolutionSecretsVersion);
                Version minVersion = new Version(Versions.MinimumFileFormatSupported);
                return headerVersion.Major <= minVersion.Major;
            }
            catch
            {
                return false;
            }
        }

    }
}

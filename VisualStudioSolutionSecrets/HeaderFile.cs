using System;
using System.Text.Json.Serialization;


namespace VisualStudioSolutionSecrets
{

    internal class HeaderFile
    {
        [JsonPropertyName("visualStudioSolutionSecretsVersion")]
        public string VisualStudioSolutionSecretsVersion { get; set; } = null!;

        [JsonPropertyName("lastUpload")]
        public DateTime LastUpload { get; set; }

        [JsonPropertyName("solutionFile")]
        public string SolutionFile { get; set; } = null!;

        [JsonPropertyName("solutionGuid")]
        public Guid? SolutionGuid { get; set; }


        public bool IsVersionSupported()
        {
            try
            {
                Version headerVersion = new(VisualStudioSolutionSecretsVersion);
                Version minVersion = new(Versions.MinFileFormatSupported);
                Version maxVersion = new(Versions.MaxFileFormatSupported);

                return minVersion.Major <= headerVersion.Major && headerVersion.Major <= maxVersion.Major;
            }
            catch
            {
                return false;
            }
        }

    }
}

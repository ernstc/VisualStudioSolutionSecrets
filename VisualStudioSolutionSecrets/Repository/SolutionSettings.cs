using System.Collections.Generic;

namespace VisualStudioSolutionSecrets.Repository
{
    public class SolutionSettings {
        public string SolutionName { get; set; } = null!;
        public ICollection<(string name, string? content)> Settings { get; set; } = null!;
    }
}

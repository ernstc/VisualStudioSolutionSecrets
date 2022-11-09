using System;
using System.Collections.Generic;

namespace VisualStudioSolutionSecrets.Repository
{
    public class SolutionSettings : ISolution
    {
        public SolutionSettings(ICollection<(string name, string? content)> settings)
        {
            Settings = settings;
        }

        public string Name { get; set; } = null!;
        public Guid Uid { get; set; }
        public ICollection<(string name, string? content)> Settings { get; } = null!;
    }
}

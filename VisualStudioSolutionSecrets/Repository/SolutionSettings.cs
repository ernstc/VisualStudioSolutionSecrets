using System;
using System.Collections.Generic;

namespace VisualStudioSolutionSecrets.Repository
{
    public class SolutionSettings : ISolution
    {
        public string Name { get; set; } = null!;
        public Guid Uid { get; set; }
        public ICollection<(string name, string? content)> Settings { get; set; } = null!;
    }
}

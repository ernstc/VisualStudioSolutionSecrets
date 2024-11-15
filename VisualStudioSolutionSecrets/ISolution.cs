using System;

namespace VisualStudioSolutionSecrets
{
    internal interface ISolution
    {
        string Name { get; }
        Guid Uid { get; }
    }
}

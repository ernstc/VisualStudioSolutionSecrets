using McMaster.Extensions.CommandLineUtils;

namespace VisualStudioSolutionSecrets.Commands.Abstractions
{
    internal class CommandBaseWithPath : CommandBase
    {
        [Argument(0, Name = "path", Description = "Path for searching solutions or single solution file path.")]
        public string? Path { get; set; }

    }
}

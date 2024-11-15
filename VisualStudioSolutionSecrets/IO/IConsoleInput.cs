using System;

namespace VisualStudioSolutionSecrets.IO
{
    internal interface IConsoleInput
    {
        ConsoleKeyInfo ReadKey();
    }
}

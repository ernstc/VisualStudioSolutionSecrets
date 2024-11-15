using System;

namespace VisualStudioSolutionSecrets.IO
{
    internal class ConsoleInput : IConsoleInput
    {
        public ConsoleKeyInfo ReadKey()
        {
            return Console.ReadKey();
        }
    }
}

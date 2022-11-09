using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisualStudioSolutionSecrets.IO
{
    public interface IConsoleInput
    {
        ConsoleKeyInfo ReadKey();
    }
}

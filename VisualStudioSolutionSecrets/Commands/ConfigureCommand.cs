using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisualStudioSolutionSecrets.Commands.Abstractions;

namespace VisualStudioSolutionSecrets.Commands
{
    internal class ConfigureCommand : Command<ConfigureOptions>
    {
        protected override async Task Execute(ConfigureOptions options)
        {
            var configuration = Configuration.Current;
        }
    }
}

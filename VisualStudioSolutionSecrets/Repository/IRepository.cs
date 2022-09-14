﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisualStudioSolutionSecrets.Repository
{

    public class SolutionSettings {
        public string SolutionName { get; set; } = null!;
        public ICollection<(string name, string? content)> Settings { get; set; } = null!;
    }


    public interface IRepository : IService
    {
        string? SolutionName { get; set; }
        Task<string?> StartDeviceFlowAuthorizationAsync();
        Task CompleteDeviceFlowAuthorizationAsync();
        Task<bool> PushFilesAsync(ICollection<(string name, string? content)> files);
        Task<ICollection<(string name, string? content)>> PullFilesAsync();
        Task<ICollection<SolutionSettings>> PullAllSecretsAsync();
    }
}
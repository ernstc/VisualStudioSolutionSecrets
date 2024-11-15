namespace VisualStudioSolutionSecrets.IO
{
    internal interface IFileSystem
    {
        string GetApplicationDataFolderPath();
        string GetCurrentDirectory();
        string GetSecretsFolderPath();
    }
}

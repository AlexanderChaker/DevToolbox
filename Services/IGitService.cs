namespace DeployGitBranch.Services;

public interface IGitService
{
    Task<List<string>> ParseYAMLAsync(string filePath);
    Task<List<string>> RunGitDiffsAsync();
    Task<List<string>> RunGitDiffsAsync(string workingDirectory, string currentBranch, string sourceBranch);
}
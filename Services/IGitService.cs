namespace DeployGitBranch.Services;

public interface IGitService
{
    Task<List<string>> GetBranchesAsync(string workingDirectory);
    Task<List<string>> ParseYAMLAsync(string filePath);
    Task<List<string>> RunGitDiffsAsync(string workingDirectory, string currentBranch, string sourceBranch);
}
namespace Services;

public interface IGitService
{
    List<string> GetAbsolutFileList(string workingDirectory, List<string> relativeFileList);
    Task<List<string>> GetBranchesAsync(string workingDirectory);
    Task<List<string>> ParseYAMLAsync(string filePath);
    Task<List<string>> RunGitDiffsAsync(string workingDirectory, string currentBranch, string sourceBranch);
    List<string> SortByYaml(string workingDirectory, List<string> yamlList, List<string> fileList);
    Task<bool> VerifyGitRepoAsync(string workingDirectory);
}
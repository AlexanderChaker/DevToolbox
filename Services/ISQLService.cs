namespace DeployGitBranch.Services;

public interface ISQLService
{
    Task<List<string>> GetDBListAsync();
    void SetConnectionString(string connectionString);
    Task<List<string>> RunSQLQueriesAsync(string serverUrl, List<string> fileList);
    List<string> GetAbsolutFileList(string workingDirectory, List<string> relativeFileList);
}
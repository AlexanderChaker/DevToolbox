namespace DeployGitBranch.Services;

public interface ISQLService
{
    int Progress { get; }

    Task<List<string>> GetDBListAsync();
    void SetConnectionString(string connectionString);
    Task<List<string>> RunSQLQueriesAsync(string serverUrl, List<string> fileList);
    Task<List<string>> RunSQLQueryAsync(string serverUrl, string filePath);
}
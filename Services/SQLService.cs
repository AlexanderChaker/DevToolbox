using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CliWrap;
using CliWrap.Buffered;
using System.Security.Principal;
using Repos;
using DevToolbox.Services.Interfaces;

namespace Services;

public class SQLService: ISQLService
{
    private MyDbContext? _myDbContext;
    private readonly ILogger<MyDbContext> _logger;

    public SQLService(MyDbContext dbContext, ILogger<MyDbContext> Logger)
    {
        _myDbContext = dbContext;
        _logger = Logger;
    }

    public SQLService(string connectionString, ILogger<MyDbContext> Logger)
    {
        var contextOptions = new DbContextOptionsBuilder<MyDbContext>()
                                .UseSqlServer(connectionString)
                                .Options;

        _myDbContext = new MyDbContext(contextOptions);
        _logger = Logger;
    }

    [ActivatorUtilitiesConstructor]
    public SQLService(ILogger<MyDbContext> Logger)
    {
        _logger = Logger;
    }

    public void SetConnectionString(string connectionString)
    {
        if (_myDbContext is not null)
        {
            _myDbContext.Dispose();
        }

        var contextOptions = new DbContextOptionsBuilder<MyDbContext>()
                                .UseSqlServer(connectionString)
                                .Options;

        _myDbContext = new MyDbContext(contextOptions);

        var dbAuthenticationUser = GetCurrentUser();
    }

    public async Task<List<string>> GetDBListAsync()
    {
        if (_myDbContext is null)
        {
            throw new Exception("Connection String needs to be set");
        }

        var databases = await _myDbContext.Database
                                .SqlQuery<string>($"SELECT CAST(name as VARCHAR(32)) as DBName FROM sys.databases WHERE state_desc = 'ONLINE'")
                                .ToListAsync();

        return databases;
    }

    //TODO: Make method return a continuous stream of logs
    
    public async Task<List<string>> RunSQLQueriesAsync(string serverUrl, List<string> fileList)
    {
        if (string.IsNullOrEmpty(serverUrl))
        {
            throw new Exception("Server Url needs to be set");
        }

        string cmd = "sqlcmd.exe";
        string args = "-S {0} -i \"{1}\"";

        List<string> results = new();

        foreach (string file in fileList)
        {
            _logger.LogInformation("Deploying to {server} file {file}", serverUrl, file);
            
            var result = await Cli.Wrap(cmd)
                                  .WithArguments(string.Format(args,serverUrl,file))
                                  .ExecuteBufferedAsync();

            var error = result.StandardError;
            if (!string.IsNullOrEmpty(error))
            {
                throw new Exception($"SQL error: {error}");
            }

            var output = result.StandardOutput;
            results.Add(output);
        }

        return results;
    }

    public async Task<List<string>> RunSQLQueryAsync(string serverUrl, string filePath)
    {
        if (string.IsNullOrEmpty(serverUrl))
        {
            throw new Exception("Server Url needs to be set");
        }

        string cmd = "sqlcmd.exe";
        string args = "-S {0} -i \"{1}\"";

        List<string> results = new();

        _logger.LogInformation("Deploying to {server} file {file}", serverUrl, filePath);

        var result = await Cli.Wrap(cmd)
                                .WithArguments(string.Format(args, serverUrl, filePath))
                                .ExecuteBufferedAsync();

        var error = result.StandardError;
        if (!string.IsNullOrEmpty(error))
        {
            throw new Exception($"SQL error: {error}");
        }

        var output = result.StandardOutput;
        results.Add(output);

        return results;
    }

    public string GetCurrentUser()
    {
        string username = string.Empty;

        #if WINDOWS
        username = WindowsIdentity.GetCurrent()?.Name??"";
        #endif

        return username;
    }
}

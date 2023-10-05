using Microsoft.EntityFrameworkCore;
using DeployGitBranch.Repos;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using CliWrap;
using CliWrap.Buffered;
using System;

namespace DeployGitBranch.Services;

public class SQLService: ISQLService
{
    private MyDbContext? _myDbContext;
    private readonly ILogger<MyDbContext> _logger;

    public SQLService(MyDbContext dbContext, ILogger<MyDbContext> Logger)
    {
        //var contextOptions = new DbContextOptionsBuilder<MyDbContext>()
        //                        .UseSqlServer(@"Server=DEVSentientSQL.flexjetnet.com;Integrated Security=true;MultipleActiveResultSets=true;Multisubnetfailover=yes;Application Name=Skynet.SentientSync.API-Dev;Encrypt=false")
        //                        .Options;
        //_myDbContext = new MyDbContext(contextOptions);

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

    public List<string> GetAbsolutFileList(string workingDirectory, List<string> relativeFileList)
    {
        if (string.IsNullOrEmpty(workingDirectory))
        {
            throw new Exception("Working Directory needs to be set");
        }

        List<string> absoluteFileList = new();

        foreach (string file in relativeFileList)
        {
            if (string.IsNullOrWhiteSpace(file))
            {
                continue;
            }

            string absoluteFilePath = new DirectoryInfo(Path.Combine(workingDirectory, file)).FullName; //This normalizes the Windows and Linux paths
            absoluteFileList.Add(absoluteFilePath);
        }

        return absoluteFileList;
    }


    //This proc was hanging when running a sql query that returns a 50k+ chars. Specifically this one ".\Sentient.Database\Scripts\142300_BWR_IsBillingAddress\99_CreateMasks.sql"
    //public async Task<List<string>> RunSQLQueriesAsync(List<string> queryList)
    //{
    //    List<string> result = new();
    //    string? error, output = "";

    //    ProcessStartInfo startInfo = new()
    //    {
    //        WindowStyle = ProcessWindowStyle.Hidden,
    //        CreateNoWindow = true,
    //        WorkingDirectory = "C:\\source_code\\Database\\Sentient.Database\\",
    //        FileName = "powershell",
    //        //Arguments = ,
    //        RedirectStandardOutput = true,
    //        RedirectStandardError = true
    //    };

    //    Process process = new();
    //    process.StartInfo = startInfo;
    //    foreach (string query in queryList)
    //    {
    //        _logger.LogInformation("Running query: {query}", query);

    //        process.StartInfo.Arguments = query;
    //        process.Start();

    //        error = process.StandardError.ReadLine();
    //        if (!string.IsNullOrEmpty(error))
    //        {
    //            throw new Exception($"Git error: {error}");
    //        }

    //        output = process.StandardOutput.ReadLine();
    //        await process.WaitForExitAsync();

    //        result.Add(output??"");
    //    }

    //    return result;
    //}

    public async Task<List<string>> RunSQLQueriesAsync(string serverUrl, List<string> fileList)
    {
        if (string.IsNullOrEmpty(serverUrl))
        {
            throw new Exception("Server Url needs to be set");
        }

        string cmd = "sqlcmd.exe";
        string args = "-S {0} -i {1}";

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
}

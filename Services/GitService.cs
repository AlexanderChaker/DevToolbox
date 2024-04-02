using CliWrap;
using CliWrap.Buffered;
using DeployGitBranch.Repos;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;

namespace DeployGitBranch.Services;

public class GitService : IGitService
{
    private readonly ILogger<MyDbContext> _logger;

    public GitService(ILogger<MyDbContext> Logger)
    {
        _logger = Logger;
    }

    public async Task<List<string>> ParseYAMLAsync(string filePath)
    {
        List<string> result = new();

        try
        {
            // Open a FileStream to read the file
            using (FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read))
            {
                // Create a StreamReader to read the data
                using (StreamReader reader = new(fileStream))
                {
                    // Read the entire file into a string
                    string fileContents = await reader.ReadToEndAsync();

                    // Close the StreamReader (which also closes the underlying FileStream)
                    reader.Close();

                    //textBlock.Text = fileContents;
                    var yamlDeserializer = new DeserializerBuilder()
                                                .IgnoreUnmatchedProperties()
                                                //.WithNamingConvention(PascalCaseNamingConvention.Instance) //TODO how to make it map case insensitive?
                                                .Build();
                    AzurePipelineYaml yamlFile = yamlDeserializer.Deserialize<AzurePipelineYaml>(fileContents);
                    string[] foldersToProcess = yamlFile.variables?["FoldersToProcess"].Split(',', StringSplitOptions.TrimEntries) ?? [];

                    foreach (var pair in foldersToProcess)
                    {
                        var parts = pair.Split(':', StringSplitOptions.TrimEntries);
                        result.Add(parts[1]);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("An error occurred: " + e.Message);
        }

        return result;
    }

    public async Task<List<string>> GetBranchesAsync(string workingDirectory)
    {
        if (string.IsNullOrWhiteSpace(workingDirectory))
        {
            throw new Exception("Working Directory needs to be set");
        }

        string cmd = "powershell";
        string args = $@"git for-each-ref --format='%(refname:short)' refs/heads/";

        var result = await Cli.Wrap(cmd)
                              .WithWorkingDirectory(workingDirectory)
                              .WithArguments(args)
                              .ExecuteBufferedAsync();

        var error = result.StandardError;
        if (!string.IsNullOrEmpty(error))
        {
            _logger.LogError(@"Git error: {error}", error);
            throw new Exception($"Git error: {error}");
        }

        var output = result.StandardOutput;

        //Parse into a list
        List<string> diffList = output.Split("\n").Where(b=>(!string.IsNullOrEmpty(b))).ToList();
        diffList.Sort();

        return diffList;
    }

    public async Task<List<string>> RunGitDiffsAsync(string workingDirectory, string currentBranch, string sourceBranch)
    {
        if (string.IsNullOrWhiteSpace(workingDirectory))
        {
            throw new Exception("Working Directory needs to be set");
        }

        await StashBranchChangesAsync(workingDirectory);
        await PullLatestBranchAsync(workingDirectory, sourceBranch);
        await PullLatestBranchAsync(workingDirectory, currentBranch);//This has to be checked out last

        string cmd = "powershell";
        string args = $@"git diff --diff-filter=d --name-only {sourceBranch} {currentBranch} | Where-Object {{$_ -like '*.sql'}}";

        var result = await Cli.Wrap(cmd)
                              .WithWorkingDirectory(workingDirectory)
                              .WithArguments(args)
                              .ExecuteBufferedAsync();

        var error = result.StandardError;
        if (!string.IsNullOrEmpty(error))
        {
            _logger.LogError(@"Git error: {error}", error);
            //throw new Exception($"Git error: {error}");
        }

        var output = result.StandardOutput;

        //Parse into a list
        List<string> diffList = output.Split("\r\n").ToList();
        
        return diffList;
    }

    public List<string> GetAbsolutFileList(string workingDirectory, List<string> relativeFileList)
    {
        if (string.IsNullOrWhiteSpace(workingDirectory))
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

    private async Task PullLatestBranchAsync(string workingDirectory, string branchName)
    {
        if (string.IsNullOrWhiteSpace(branchName))
        {
            throw new Exception("Branch Name invalid");
        }

        _logger.LogInformation(@"Pulling latest branch: {branchName}", branchName);

        string cmd = "powershell";
        string args = $@"git checkout {branchName}; git pull --quiet;";

        var result = await Cli.Wrap(cmd)
                              .WithWorkingDirectory(workingDirectory)
                              .WithArguments(args)
                              .ExecuteBufferedAsync();

        var error = result.StandardError;
        if (!string.IsNullOrEmpty(error))
        {
            _logger.LogError(@"Git error: {error}", error);
        }

        var output = result.StandardOutput;
        _logger.LogInformation(output);
    }

    private async Task StashBranchChangesAsync(string workingDirectory)
    {
        if (string.IsNullOrWhiteSpace(workingDirectory))
        {
            throw new Exception("Working Directory needs to be set");
        }

        _logger.LogInformation(@"Stashing uncommitted changes");

        string stashMsg = "Stashed by DeployGitBranch app";
        string cmd = "powershell";
        string[] args = ["git", "stash", "-m", $"\"{stashMsg}\""];

        var result = await Cli.Wrap(cmd)
                              .WithWorkingDirectory(workingDirectory)
                              .WithArguments(args)
                              .WithValidation(CommandResultValidation.None)
                              .ExecuteBufferedAsync();

        var error = result.StandardError;
        if (!string.IsNullOrEmpty(error))
        {
            _logger.LogError(@"Git error: {error}", error);
        }

        var output = result.StandardOutput;
        _logger.LogInformation(output);
    }

    public async Task<bool> VerifyGitRepoAsync(string workingDirectory)
    {
        if (string.IsNullOrWhiteSpace(workingDirectory))
        {
            throw new Exception("Working Directory needs to be set");
        }

        string cmd = "powershell";
        string args = $@"git status --short;";

        var result = await Cli.Wrap(cmd)
                      .WithWorkingDirectory(workingDirectory)
                      .WithArguments(args)
                      .WithValidation(CommandResultValidation.None)
                      .ExecuteBufferedAsync();

        var error = result.StandardError;
        if (!string.IsNullOrEmpty(error))
        {
            _logger.LogError(@"Git error: {error}", error);
            return false;
        }
        else
        {
            var output = result.StandardOutput;
            _logger.LogInformation(@"Git repo verified: {output}", output);

            return true;
        }
    }

    public List<string> SortByYaml(string workingDirectory, List<string> yamlList, List<string> fileList)
    {
        List<string> result = new();

        foreach (var folder in yamlList)
        {
            var fullFolderPath = Path.Combine(workingDirectory, folder);

            if (string.IsNullOrEmpty(folder))
            {
                break;
            }

            foreach(string file in fileList.Where(f=>f.StartsWith(fullFolderPath)))
            {
                result.Add(file);
            }
        }

        return result;
    }

    private class AzurePipelineYaml
    {
        public Dictionary<string, string>? variables { get; set; }
    }
}

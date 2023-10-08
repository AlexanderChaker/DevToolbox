using CliWrap;
using CliWrap.Buffered;
using YamlDotNet.Serialization;

namespace DeployGitBranch.Services;

public class GitService : IGitService
{
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
        if (string.IsNullOrEmpty(workingDirectory))
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
            throw new Exception($"Git error: {error}");
        }

        var output = result.StandardOutput;

        //Parse into a list
        List<string> diffList = output.Split("\n").Where(b=>(!string.IsNullOrEmpty(b))).ToList();

        return diffList;
    }

    public async Task<List<string>> RunGitDiffsAsync(string workingDirectory, string currentBranch, string sourceBranch)
    {
        if (string.IsNullOrEmpty(workingDirectory))
        {
            throw new Exception("Working Directory needs to be set");
        }

        string cmd = "powershell";
        string args = $@"git diff --diff-filter=d --name-only {sourceBranch} {currentBranch} | Where-Object {{$_ -like '*.sql'}}";

        var result = await Cli.Wrap(cmd)
                              .WithWorkingDirectory(workingDirectory)
                              .WithArguments(args)
                              .ExecuteBufferedAsync();

        var error = result.StandardError;
        if (!string.IsNullOrEmpty(error))
        {
            throw new Exception($"Git error: {error}");
        }

        var output = result.StandardOutput;

        //Parse into a list
        List<string> diffList = output.Split("\r\n").ToList();
        
        return diffList;
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

    private class AzurePipelineYaml
    {
        public Dictionary<string, string>? variables { get; set; }
    }
}

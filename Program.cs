using CliWrap;
using CommandLine;
using System.Text;


var result = Parser.Default.ParseArguments<Options>(args);
if (result.Errors.Any())
{
    return;
}

var options = result.Value;

options.RepositoryPath ??= Directory.GetCurrentDirectory();

options.TargetBranch ??= await GetCurrentBranchAsync();

Console.WriteLine(options.MainBranch);
Console.WriteLine(options.TargetBranch);
Console.WriteLine(options.RepositoryPath);
Console.WriteLine(options.Comments.Count());

var main = options.MainBranch;
string target = options.TargetBranch;
var squashBranch = $"{target}-squash";
var comments = string.Join(' ', options.Comments.Select(comment => $"-m {comment}"));
var remote = options.Remote;

await RunGitCommandAsync($"checkout {target}");
await RunGitCommandAsync("pull");
await RunGitCommandAsync($"checkout {main}");
await RunGitCommandAsync("pull");
await RunGitCommandAsync($"checkout {target}");
await RunGitCommandAsync($"merge --commit {main}");

await RunGitCommandAsync($"checkout {main}");
await RunGitCommandAsync($"checkout -b {squashBranch}");
await RunGitCommandAsync($"merge --squash {target}");
await RunGitCommandAsync($"commit {comments}");

await RunGitCommandAsync($"branch -d {target}");
await RunGitCommandAsync($"push -d {remote} {target}");
await RunGitCommandAsync($"branch -m {target}");
await RunGitCommandAsync($"push");


async Task<string> GetCurrentBranchAsync()
{
    return await RunGitCommandAsync("rev-parse --abbrev-ref HEAD");
}

async Task<string> RunGitCommandAsync(string arguments)
{
    var outputBuilder = new StringBuilder();
    await Cli.Wrap("git")
        .WithArguments(arguments)
        .WithWorkingDirectory(options.RepositoryPath)
        .WithStandardOutputPipe(PipeTarget.ToStringBuilder(outputBuilder, Console.OutputEncoding))
        .WithStandardErrorPipe(PipeTarget.ToDelegate(Console.WriteLine))
        .ExecuteAsync();

    return outputBuilder.ToString().Trim();
}

class Options
{
    [Option('m', "main", Required = false, HelpText = "Set the main (master) branch.", Default = "master")]
    public string MainBranch { get; set; } = null!;

    [Option('t', "target", Required = false, HelpText = "Set the target branch.", Default = null)]
    public string? TargetBranch { get; set; }

    [Option('p', "path", Required = false, HelpText = "Set path to the git repository.", Default = null)]
    public string? RepositoryPath { get; set; }

    [Option('r', "remote", Required = false, HelpText = "Set the name of the remote.", Default = "origin")]
    public string Remote { get; set; } = null!;

    [Option('c', "comment", Required = true, HelpText = "Comment for squash commit. Can be multiple.", Min = 1)]
    public IEnumerable<string> Comments { get; set; } = null!;
}
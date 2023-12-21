// See https://aka.ms/new-console-template for more information
using CliWrap;
using CommandLine;
using System.Text;

Console.WriteLine("Hello, World!");

var testArgs = args.Concat(new[] { "Test comment" }).ToArray();

var result = Parser.Default.ParseArguments<Options>(testArgs);
if (result.Errors.Any())
{
    Console.WriteLine("Errors:");
    foreach (var error in result.Errors)
    {
        Console.WriteLine(error.Tag);
    }
}

var options = result.Value;

options.RepositoryPath ??= Directory.GetCurrentDirectory();

options.TargetBranch ??= await GetCurrentBranchAsync(options.RepositoryPath);

Console.WriteLine(options.MainBranch);
Console.WriteLine(options.TargetBranch);
Console.WriteLine(options.Comment);


static async Task<string> GetCurrentBranchAsync(string path)
{
    return await RunGitCommandAsync(path, "rev-parse --abbrev-ref HEAD");
}

static async Task<string> RunGitCommandAsync(string path, string arguments)
{
    var outputBuilder = new StringBuilder();
    await Cli.Wrap("git")
        .WithArguments(arguments)
        .WithWorkingDirectory(path)
        .WithStandardOutputPipe(PipeTarget.ToStringBuilder(outputBuilder, Console.OutputEncoding))
        .WithStandardErrorPipe(PipeTarget.ToDelegate(Console.WriteLine))
        .ExecuteAsync();

    return outputBuilder.ToString();
}

class Options
{
    [Option('m', "main", Required = false, HelpText = "Set the main branch.", Default = "master")]
    public string MainBranch { get; set; } = null!;

    [Option('t', "target", Required = false, HelpText = "Set the target branch.", Default = null)]
    public string? TargetBranch { get; set; }

    [Option('p', "path", Required = false, HelpText = "Set path to the git repository.", Default = null)]
    public string? RepositoryPath { get; set; }

    [Value(0, Required = true, HelpText = "Comment for squash commit.")]
    public string Comment { get; set; } = null!;
}
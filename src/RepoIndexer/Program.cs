using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RepoIndexer.Services;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: repo-indexer <root-folder>");
    return 1;
}

var rootFolder = Path.GetFullPath(args[0]);

if (!Directory.Exists(rootFolder))
{
    Console.Error.WriteLine($"Root folder not found: {rootFolder}");
    return 1;
}

Console.WriteLine($"Starting repo-indexer. Watching: {rootFolder}");

var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Information);
    })
    .ConfigureServices((_, services) =>
    {
        services.AddSingleton<RoslynAnalyzerService>();
        services.AddSingleton<RepoIndexingService>();
        services.AddSingleton<RepoDiscoveryService>();
        services.AddSingleton(new WatcherOptions { RootFolder = rootFolder });
        services.AddHostedService<RepoWatcherService>();
    })
    .Build();

await host.RunAsync();
return 0;

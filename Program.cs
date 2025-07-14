using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CricketScoreReader;

class Program
{
    static async Task Main(string[] args)
    {
        using var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((_, config) =>
            {
                config.AddJsonFile("configs/appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                services.Configure<PipelineConfig>(context.Configuration.GetSection("PipelineConfig"));
                services.Configure<OcrConfig>(context.Configuration.GetSection("OcrConfig"));

                services.AddSingleton<IFrameProcessorFactory, OcrProcessorFactory>();

                services.AddSingleton<IGameParser, GameParser>();
                services.AddSingleton<IFramePipeline, FramePipeline>();
            })
            .Build();


        var pipeline = host.Services.GetRequiredService<IFramePipeline>();
        var cts = new CancellationTokenSource();

        var stopwatch = Stopwatch.StartNew();
        var runTask = pipeline.RunAsync(cts.Token);
        await runTask;
        var parser = host.Services.GetRequiredService<IGameParser>();
        MatchFileWriter.WriteBallRecordsWithTotalRunsAndWickets(parser.Results, "match_results.csv");
        // For audit
        MatchFileWriter.WriteHistory(parser.History, "match_history.csv");
        stopwatch.Stop();
        Console.WriteLine("Done！ Total time: {0} seconds. Press any key to exit.", stopwatch.Elapsed.TotalSeconds);
        Console.ReadKey();
    }
}
using System.Diagnostics;
using CricketScoreReader;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

class Program
{
    static async Task Main(string[] args)
    {
        using var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.Configure<PipelineConfig>(cfg =>
                {
                    cfg.VideoPath = "match-origin.mp4";
                    cfg.IntervalSeconds = 2;
                });
                services.Configure<OcrConfig>(cfg =>
                {
                    cfg.TessDataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");
                    cfg.RoiConfigPath = "roi_config.json";
                });
                services.AddSingleton<IFrameProcessorFactory, FrameProcessorFactory>();

                services.AddSingleton<IGameParser, GameParser>();
                services.AddSingleton<IFramePipeline, FramePipeline>();
            })
            .Build();


        var pipeline = host.Services.GetRequiredService<IFramePipeline>();
        var cts = new CancellationTokenSource();
        

        var stopwatch = Stopwatch.StartNew();
        var runTask = pipeline.RunAsync(cts.Token,1, 6);

        await runTask;

        var parser = host.Services.GetRequiredService<IGameParser>();
        MatchRecorder.WriteResult(parser.Results, "match_results.csv");
        MatchRecorder.WriteHistory(parser.History, "match_history.csv");
        stopwatch.Stop();
        Console.WriteLine("Done！ Total time: {0} seconds", stopwatch.Elapsed.TotalSeconds);
    }
}

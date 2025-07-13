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

                services.AddSingleton<GameParser>();
                services.AddSingleton<FramePipeline>();
            })
            .Build();


        var pipeline = host.Services.GetRequiredService<FramePipeline>();
        var cts = new CancellationTokenSource();

        var runTask = pipeline.RunAsync(cts.Token, 3);

        await runTask;

        var parser = host.Services.GetRequiredService<GameParser>();
        MatchRecorder.WriteResult(parser.Results, "match_results.csv");
        MatchRecorder.WriteHistory(parser.History, "match_history.csv");
        Console.WriteLine("Done！");
    }
}

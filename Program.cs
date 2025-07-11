using System;
using System.Threading;
using CricketScoreReader;

class Program
{
    static async Task Main(string[] args)
    {
        string videoPath = "7-1.mp4";
        string roiConfigPath = "roi_config.json";
        string tessdataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");

        var processor = new FrameProcessor(tessdataPath, roiConfigPath);
        var parser = new GameParser();

        var pipeline = new FramePipeline(videoPath, processor, parser, 2);
        var cts = new CancellationTokenSource();

        Console.WriteLine("按下任意鍵停止...");
        var runTask = pipeline.RunAsync(cts.Token, 1);

        Console.ReadKey();
        cts.Cancel();

        await runTask;

        MatchRecorder.WriteCsv(parser.Results, "match_results.csv");
        Console.WriteLine("處理完成！");
    }
}

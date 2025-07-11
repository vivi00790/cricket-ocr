using CricketScoreReader;

class Program
{
    static async Task Main(string[] args)
    {
        var videoPath = "7-1.mp4";
        var roiConfigPath = "roi_config.json";
        var tessdataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");

        var processor = new FrameProcessor(tessdataPath, roiConfigPath);
        var parser = new GameParser();

        var pipeline = new FramePipeline(videoPath, processor, parser, 2);
        var cts = new CancellationTokenSource();

        var runTask = pipeline.RunAsync(cts.Token, 1);

        await runTask;

        MatchRecorder.WriteCsv(parser.Results, "match_results.csv");
        MatchRecorder.WriteCsv(parser.History, "match_history.csv");
        Console.WriteLine("Done！");
    }
}

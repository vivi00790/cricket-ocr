using Microsoft.Extensions.Options;

namespace CricketScoreReader;

using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using OpenCvSharp;

public interface IFramePipeline
{
    Task RunAsync(CancellationToken token);
}

public class FramePipeline : IFramePipeline
{
    private readonly Channel<FrameItem> _channel;

    private readonly IGameParser _parser;

    private readonly IFrameProcessorFactory _processorFactory;
    private readonly string _valueVideoPath;
    private readonly int _valueIntervalSeconds;
    private readonly int _producerCount;
    private readonly int _consumerCount;

    public FramePipeline(
        IOptions<PipelineConfig> config,
        IFrameProcessorFactory processorFactory,
        IGameParser parser)
    {
        _channel = Channel.CreateBounded<FrameItem>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait
        });

        _processorFactory = processorFactory;
        _parser = parser;
        _valueVideoPath = config.Value.VideoPath;
        _valueIntervalSeconds = config.Value.SamplingIntervalSeconds;
        _producerCount = config.Value.producerCount;
        _consumerCount = config.Value.consumerCount;
    }

    public async Task RunAsync(CancellationToken token)
    {
        Console.WriteLine("Starting frame processing pipeline...");
        var producerTasks = Enumerable.Range(0, _producerCount)
            .Select(i => Task.Run(() => ProduceAsync(i, _producerCount, token), token))
            .ToArray();
        var consumerTasks = Enumerable.Range(0, _consumerCount)
            .Select(i => Task.Run(() => ConsumeAsync(i, token), token))
            .ToArray();

        await Task.WhenAll(producerTasks);
        Console.WriteLine("All producer finished, completing channel.");
        _channel.Writer.Complete();
        await Task.WhenAll(consumerTasks);
    }

    private async Task ProduceAsync(int producerId, int producerCount, CancellationToken token)
    {
        Console.WriteLine($"Producer {producerId} started.");
        var capture = new VideoCapture(_valueVideoPath);
        if (!capture.IsOpened())
        {
            Console.WriteLine("Failed to open video file.");
            return;
        }
        var fps = capture.Get(VideoCaptureProperties.Fps);
        var intervalFrames = (int)(fps * _valueIntervalSeconds);
        Console.WriteLine("frame count:" + capture.Get(VideoCaptureProperties.FrameCount));
        for (var f = producerId * intervalFrames; f < capture.Get(VideoCaptureProperties.FrameCount); f += producerCount * intervalFrames)
        {
            if (token.IsCancellationRequested) break;

            capture.Set(VideoCaptureProperties.PosFrames, f);
            var frame = new Mat();
            if (capture.Read(frame))
            {
                if (frame.Empty())
                {
                    Console.WriteLine($"frame {f} empty!!");
                    continue;
                }

                var item = new FrameItem
                {
                    Frame = frame.Clone(),
                    FrameNumber = f,
                    Timestamp = DateTime.UtcNow
                };
                await _channel.Writer.WriteAsync(item, token);
            }
        }
        Console.WriteLine("Producer completed.");
    }

    private async Task ConsumeAsync(int workerId, CancellationToken token)
    {
        await foreach (var item in _channel.Reader.ReadAllAsync(token))
        {
            Console.WriteLine(
                $"Worker {workerId} processing frame {item.FrameNumber} at {item.Timestamp:HH:mm:ss.fff}");
            // engine cannot share across threads, so create a new instance for each frame
            var ocrData = _processorFactory.Create().ProcessFrame(item.Frame);
            if (ocrData.ContainsKey("BattingTeam"))
            {
                _parser.ProcessFrameData(new FrameData
                {
                    RunsWithWickets = ocrData.GetValueOrDefault("RunsWickets", "999/999"),
                    OversWithBalls = ocrData.GetValueOrDefault("OverWithBall", "999.999"),
                    Batter1 = ocrData.GetValueOrDefault("Batter1", "Unknown"),
                    Batter2 = ocrData.GetValueOrDefault("Batter2", "Unknown"),
                    Bowler = ocrData.GetValueOrDefault("Bowler", "Unknown"),
                    BattingTeam = ocrData.GetValueOrDefault("BattingTeam", "Unknown"),
                }, item.FrameNumber, item.Timestamp);
            }

            item.Frame.Dispose();
        }

        Console.WriteLine($"Worker {workerId} completed processing.");
    }
}
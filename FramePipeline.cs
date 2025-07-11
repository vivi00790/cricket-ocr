namespace CricketScoreReader;

using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using OpenCvSharp;

public class FramePipeline
{
    private readonly Channel<FrameItem> _channel;
    private readonly VideoCapture _capture;
    private readonly FrameProcessor _processor;
    private readonly GameParser _parser;
    private readonly int _intervalFrames;

    public FramePipeline(string videoPath, FrameProcessor processor, GameParser parser, int intervalSeconds = 2)
    {
        _channel = Channel.CreateBounded<FrameItem>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait
        });
        _capture = new VideoCapture(videoPath);
        _processor = processor;
        _parser = parser;

        var fps = _capture.Get(VideoCaptureProperties.Fps);
        _intervalFrames = (int)(fps * intervalSeconds);
    }

    public async Task RunAsync(CancellationToken token, int consumerCount = 2)
    {
        Console.WriteLine("Starting frame processing pipeline...");
        var producerTask = Task.Run(() => ProduceAsync(token));
        var consumerTasks = Enumerable.Range(0, consumerCount)
            .Select(i => Task.Run(() => ConsumeAsync(i, token)))
            .ToArray();

        await producerTask;
        await Task.WhenAll(consumerTasks);
    }

    private async Task ProduceAsync(CancellationToken token)
    {
        Console.WriteLine("frame count:"+_capture.Get(VideoCaptureProperties.FrameCount));
        for (var f = 0; f < _capture.Get(VideoCaptureProperties.FrameCount); f += _intervalFrames)
        {
            if (token.IsCancellationRequested) break;

            _capture.Set(VideoCaptureProperties.PosFrames, f);
            var frame = new Mat();
            if (_capture.Read(frame))
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
                Console.WriteLine($"producing frame {f} at {item.Timestamp:HH:mm:ss.fff}");
                await _channel.Writer.WriteAsync(item, token);
            }
        }
        Console.WriteLine("No more frames to process, completing channel.");
        _channel.Writer.Complete();
        Console.WriteLine("Producer completed.");
    }

    private async Task ConsumeAsync(int workerId, CancellationToken token)
    {
        await foreach (var item in _channel.Reader.ReadAllAsync(token))
        {
            Console.WriteLine($"Worker {workerId} processing frame {item.FrameNumber} at {item.Timestamp:HH:mm:ss.fff}");
            var ocrData = _processor.ProcessFrame(item.Frame);
            if (ocrData["BattingTeam"].Length ==3 && ocrData["BattingTeam"].Any(char.IsLetter) && ocrData["BattingTeam"].All(c => !char.IsLetter(c) || char.IsUpper(c))) // 如果 BattingTeam 是 999，則跳過這一幀
            {
                _parser.ProcessFrameData(new FrameData
                {
                    RunsWickets = ocrData.GetValueOrDefault("RunsWickets", "999"),
                    Runs1Digit = ocrData.GetValueOrDefault("Runs1Digit", "999"),
                    Runs2Digits = ocrData.GetValueOrDefault("Runs2Digits", "999"),
                    Runs3Digits = ocrData.GetValueOrDefault("Runs3Digits", "999"),
                    Runs1DigitWickets = ocrData.GetValueOrDefault("Runs1DigitWickets", "999"),
                    Runs2DigitsWickets = ocrData.GetValueOrDefault("Runs2DigitsWickets", "999"),
                    Runs3DigitsWickets = ocrData.GetValueOrDefault("Runs3DigitsWickets", "999"),
                    Over = ocrData.GetValueOrDefault("Over", "999"),
                    Ball = ocrData.GetValueOrDefault("Ball", "999"),
                    OverWithBall = ocrData.GetValueOrDefault("OverWithBall", "999"),
                    Batter1 = ocrData.GetValueOrDefault("Batter1", "Unknown"),
                    Batter2 = ocrData.GetValueOrDefault("Batter2", "Unknown"),
                    Bowler = ocrData.GetValueOrDefault("Bowler", "Unknown"),
                    BattingTeam = ocrData.GetValueOrDefault("BattingTeam", "Unknown")
                }, item.FrameNumber, item.Timestamp);
            }
            item.Frame.Dispose();
        }
        Console.WriteLine($"Worker {workerId} completed processing.");
    }
}
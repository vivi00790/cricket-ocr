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
            _parser.ProcessFrameData(ocrData, item.FrameNumber, item.Timestamp);
            item.Frame.Dispose();
        }
        Console.WriteLine($"Worker {workerId} completed processing.");
    }
}
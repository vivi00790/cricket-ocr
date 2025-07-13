using Microsoft.Extensions.Options;

namespace CricketScoreReader;

public class FrameProcessorFactory : IFrameProcessorFactory
{
    private readonly string _tessdataPath;
    private readonly string _roiPath;

    public FrameProcessorFactory(IOptions<OcrConfig> ocrConfig)
    {
        _tessdataPath = ocrConfig.Value.TessDataPath;
        _roiPath = ocrConfig.Value.RoiConfigPath;
    }

    public FrameProcessor Create()
    {
        return new FrameProcessor(_tessdataPath, _roiPath); // 每次產生新的 engine
    }
}

public interface IFrameProcessorFactory
{
    FrameProcessor Create();
}
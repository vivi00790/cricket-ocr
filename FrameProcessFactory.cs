using Microsoft.Extensions.Options;

namespace CricketScoreReader;

public class FrameProcessorFactory(IOptions<OcrConfig> ocrConfig) : IFrameProcessorFactory
{
    public FrameProcessor Create()
    {
        return new FrameProcessor(ocrConfig.Value.TessDataPath, ocrConfig.Value.RoiConfigPath);
    }
}

public interface IFrameProcessorFactory
{
    FrameProcessor Create();
}
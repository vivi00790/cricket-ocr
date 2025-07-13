using Microsoft.Extensions.Options;

namespace CricketScoreReader;

public class OcrProcessorFactory(IOptions<OcrConfig> ocrConfig) : IFrameProcessorFactory
{
    public OcrProcessor Create()
    {
        return new OcrProcessor(ocrConfig.Value.TessDataPath, ocrConfig.Value.RoiConfigPath);
    }
}

public interface IFrameProcessorFactory
{
    OcrProcessor Create();
}
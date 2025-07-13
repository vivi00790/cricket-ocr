namespace CricketScoreReader;

public class PipelineConfig
{
    public string VideoPath { get; set; } = "";
    public int SamplingIntervalSeconds { get; set; } = 2;
    public int producerCount { get; set; } = 1;
    public int consumerCount { get; set; } = 6;
}

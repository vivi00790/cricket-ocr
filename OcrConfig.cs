namespace CricketScoreReader;

public class OcrConfig
{
    public string TessDataPath { get; set; }
    public string Language { get; set; } = "eng";
    public string RoiConfigPath { get; set; }
}
using OpenCvSharp;

namespace CricketScoreReader;

public class FrameItem
{
    public Mat Frame { get; set; }
    public int FrameNumber { get; set; }
    public DateTime Timestamp { get; set; }
}
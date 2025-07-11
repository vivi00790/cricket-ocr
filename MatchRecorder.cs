namespace CricketScoreReader;

using System.Collections.Generic;
using System.IO;

public static class MatchRecorder
{
    public static void WriteCsv(List<BallResult> results, string path)
    {
        using var writer = new StreamWriter(path);
        writer.WriteLine("Over,Ball,Runs,Wickets,FrameNumber,Timestamp");
        foreach (var r in results)
            writer.WriteLine($"{r.OverNumber},{r.BallNumber},{r.Runs},{r.Wickets},{r.FrameNumber},{r.Timestamp:O}");
    }
}
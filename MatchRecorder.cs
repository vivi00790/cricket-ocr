namespace CricketScoreReader;

using System.Collections.Generic;
using System.IO;

public static class MatchRecorder
{
    public static void WriteHistory(List<BallResult> results, string path)
    {
        using var writer = new StreamWriter(path);
        writer.WriteLine("Over,Ball,Runs,Wickets,FrameNumber, Seconds,Timestamp,Batter1,Batter2,BattingTeam,Bowler");
        foreach (var r in results)
            writer.WriteLine($"{r.OverNumber},{r.BallNumber},{r.Runs},{r.Wickets},{r.FrameNumber},{r.FrameNumber/30},{r.Timestamp:O},{r.Batter1},{r.Batter2},{r.BattingTeam},{r.Bowler}");
    }
    
    public static void WriteResult(List<BallResult> results, string path)
    {
        var currentRuns = 0;
        var currentWickets = 0;
        var summary = new List<(BallResult ballResult, int Runs, int Wickets)>();
        foreach (var item in results)
        {
            if(item is { OverNumber: 0, BallNumber: 0 })
            {
                currentRuns = 0;
                currentWickets = 0;
            }

            currentRuns += item.Runs;
            currentWickets += item.Wickets;
            summary.Add(new ValueTuple<BallResult, int, int>(item, currentRuns, currentWickets));
        }
        using var writer = new StreamWriter(path);
        writer.WriteLine("Bowler Name, Batter 1 Name, Batter 2 Name, Result - Runs, Result - Wickets, Total Runs, Total Wickets");
        foreach (var r in summary)
            writer.WriteLine($"{r.ballResult.Bowler},{r.ballResult.Batter1},{r.ballResult.Batter2},{r.ballResult.Runs},{r.ballResult.Wickets},{r.Runs},{r.Wickets}");
    }
}
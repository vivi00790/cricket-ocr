using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace CricketScoreReader;

using System;
using System.Collections.Generic;
using System.Linq;

public class GameParser
{
    private readonly object _lock = new();
    private readonly List<BallResult> _results = [];
    private readonly List<BallResult> _history = [];

    public List<BallResult> Results
    {
        get { lock (_lock) { return _results.OrderBy(r => r.FrameNumber).ToList(); } }
    }
    public List<BallResult> History
    {
        get { lock (_lock) { return _history.OrderBy(r => r.FrameNumber).ToList(); } }
    }
    

    private int? _lastRuns = null;
    private int? _lastWickets = null;
    private int _currentOver = 0;
    private int _currentBall = 0;

    public void ProcessFrameData(FrameData frameData, int frameNumber, DateTime timestamp)
    {
        if (!TryParseScore(frameData.RunsWickets, out int runs, out int wickets)) return;
        if (!TryParseOvers(frameData.OverWithBall, out int over, out int ball)) return;
        _history.Add(new BallResult
        {
            OverNumber = over,
            BallNumber = ball,
            Runs = runs,
            Wickets = wickets,
            FrameNumber = frameNumber,
            Timestamp = timestamp,
            Batter1 = frameData.Batter1,
            Batter2 = frameData.Batter2,
            BattingTeam = frameData.BattingTeam,
            Bowler = frameData.Bowler
        });
        
        lock (_lock)
        {
            Console.WriteLine($"Processing frame data=>runs:{runs}, wickets:{wickets}, over:{over}, ball:{ball}, second:{frameNumber}, timestamp:{timestamp}");
            if (_lastRuns == null || _lastWickets == null)
            {
                _lastRuns = runs;
                _lastWickets = wickets;
                _currentOver = over;
                _currentBall = ball;
                return;
            }

            if (over != _currentOver || ball != _currentBall)
            {
                var deltaRuns = Math.Max(0, runs - _lastRuns.Value);
                var deltaWickets = Math.Max(0, wickets - _lastWickets.Value);

                _results.Add(new BallResult
                {
                    OverNumber = over,
                    BallNumber = ball,
                    Runs = deltaRuns,
                    Wickets = deltaWickets,
                    FrameNumber = frameNumber,
                    Timestamp = timestamp,
                    Batter1 = frameData.Batter1,
                    Batter2 = frameData.Batter2,
                    BattingTeam = frameData.BattingTeam,
                    Bowler = frameData.Bowler
                });

                _lastRuns = runs;
                _lastWickets = wickets;
                _currentOver = over;
                _currentBall = ball;
            }
        }
    }
    
    private bool TryParseScore(string text, out int runs, out int wickets)
    {
        runs = wickets = 0;
        var match = Regex.Match(text ?? "", @"(\d+)\s*/\s*(\d+)");
        if (!match.Success) return false;

        runs = int.Parse(match.Groups[1].Value);
        wickets = int.Parse(match.Groups[2].Value);
        return true;
    }

    private bool TryParseOvers(string text, out int over, out int ball)
    {
        over = ball = 0;
        var match = Regex.Match(text ?? "", @"(\d+)\.(\d+)");
        if (!match.Success) return false;

        over = int.Parse(match.Groups[1].Value);
        ball = int.Parse(match.Groups[2].Value);
        return true;
    }

}

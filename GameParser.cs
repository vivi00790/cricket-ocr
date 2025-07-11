namespace CricketScoreReader;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public class GameParser
{
    private readonly object _lock = new();
    private readonly List<BallResult> _results = new();

    public List<BallResult> Results
    {
        get { lock (_lock) { return _results.OrderBy(r => r.FrameNumber).ToList(); } }
    }

    private int? _lastRuns = null;
    private int? _lastWickets = null;
    private int _currentOver = 0;
    private int _currentBall = 0;

    public void ProcessFrameData(Dictionary<string, string> frameData, int frameNumber, DateTime timestamp)
    {
        if (!TryParseScore(frameData.GetValueOrDefault("RunsWickets"), out int runs, out int wickets)) return;
        if (!TryParseOvers(frameData.GetValueOrDefault("Overs"), out int over, out int ball)) return;

        lock (_lock)
        {
            Console.WriteLine($"Processing frame data=>runs:{runs}, wickets:{wickets}, over:{over}, ball:{ball}, second:{frameNumber/30}, timestamp:{timestamp}");
            if (_lastRuns == null || _lastWickets == null)
            {
                _lastRuns = runs;
                _lastWickets = wickets;
                _currentOver = over;
                _currentBall = ball;
                return;
            }

            //if (over != _currentOver || ball != _currentBall)
            {
                int deltaRuns = Math.Max(0, runs - _lastRuns.Value);
                int deltaWickets = Math.Max(0, wickets - _lastWickets.Value);

                _results.Add(new BallResult
                {
                    OverNumber = over,
                    BallNumber = ball,
                    Runs = deltaRuns,
                    Wickets = deltaWickets,
                    FrameNumber = frameNumber,
                    Timestamp = timestamp
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

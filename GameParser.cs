using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace CricketScoreReader;

using System;
using System.Collections.Generic;
using System.Linq;

public class GameParser
{
    private readonly Lock _lock = new();
    private readonly List<BallResult> _results = [];
    private readonly List<BallResult> _history = [];

    public List<BallResult> Results
    {
        get
        {
            lock (_lock)
            {
                int? _lastRuns = null;
                int? _lastWickets = null;
                int _currentOver = 0;
                int _currentBall = 0;

                var ballResults = _history.OrderBy(r => r.FrameNumber).ToList();
                for (var i = 0; i < ballResults.Count; i++)
                {
                    var currentFrameData = ballResults[i];
                    var runs = currentFrameData.Runs;
                    var wickets = currentFrameData.Wickets;
                    var over = currentFrameData.OverNumber;
                    var ball = currentFrameData.BallNumber;
                    var frameNumber = currentFrameData.FrameNumber;
                    var timestamp = currentFrameData.Timestamp;
                    Console.WriteLine(
                        $"Processing frame data=>runs:{runs}, wickets:{wickets}, over:{over}, ball:{ball}, second:{frameNumber}, timestamp:{timestamp}");
                    if (_lastRuns == null || _lastWickets == null)
                    {
                        _lastRuns = runs;
                        _lastWickets = wickets;
                        _currentOver = over;
                        _currentBall = ball;
                    }

                    var deltaRuns = Math.Max(0, runs - _lastRuns.Value);
                    var deltaWickets = Math.Max(0, wickets - _lastWickets.Value);

                    // Only add a new result if either over or ball has changed
                    if (over != _currentOver || ball != _currentBall)
                    {
                        // new innings
                        if (over == 0 && ball == 0 && _currentOver != 0 && _currentBall != 0)
                        {
                            // Reset last runs and wickets for new innings
                            _lastRuns = 0;
                            _lastWickets = 0;
                            _currentOver = 0;
                            _currentBall = 0;
                        }

                        _results.Add(new BallResult
                        {
                            OverNumber = over,
                            BallNumber = ball,
                            Runs = deltaRuns,
                            Wickets = deltaWickets,
                            FrameNumber = frameNumber,
                            Timestamp = timestamp,
                            Batter1 = currentFrameData.Batter1,
                            Batter2 = currentFrameData.Batter2,
                            BattingTeam = currentFrameData.BattingTeam,
                            Bowler = currentFrameData.Bowler
                        });

                        _lastRuns = runs;
                        _lastWickets = wickets;
                        _currentOver = over;
                        _currentBall = ball;
                    }
                    // if over or ball has not changed, handle wicket.
                    else if (wickets > _lastWickets)
                    {
                        
                        // delete last record and insert wicket record with last batter because wickets only change after player region changes(in next time scoreboard appears)
                        _results.RemoveAt(_results.Count - 1);
                        _results.Add(new BallResult
                        {
                            OverNumber = over,
                            BallNumber = ball,
                            Runs = deltaRuns,
                            Wickets = wickets - _lastWickets.Value,
                            FrameNumber = frameNumber,
                            Timestamp = timestamp,
                            Batter1 = ballResults[i-1].Batter1,
                            Batter2 = ballResults[i-1].Batter2,
                            BattingTeam = ballResults[i-1].BattingTeam,
                            Bowler = ballResults[i-1].Bowler
                        });

                        _lastWickets = wickets;
                    }
                }
            }

            return _results;
        }
    }   

public List < BallResult > History
{
    get {
        lock (_lock)
        {
            return _history.OrderBy(r => r.FrameNumber).ToList();
        }
    }
}

public void ProcessFrameData(FrameData frameData, int frameNumber, DateTime timestamp)
{
    if (!TryParseScore(frameData.RunsWickets, out var runs, out var wickets)) return;
    if (!TryParseOvers(frameData.OverWithBall, out var over, out var ball)) return;

    lock (_lock)
    {
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
    }
}

private static bool TryParseScore(string text, out int runs, out int wickets)
{
    runs = wickets = 0;
    var match = Regex.Match(text ?? "", @"(\d+)\s*/\s*(\d+)");
    if (!match.Success) return false;

    runs = int.Parse(match.Groups[1].Value);
    wickets = int.Parse(match.Groups[2].Value);
    return true;
}

private static bool TryParseOvers(string text, out int over, out int ball)
{
    over = ball = 0;
    var match = Regex.Match(text ?? "", @"(\d+)\.(\d+)");
    if (!match.Success) return false;

    over = int.Parse(match.Groups[1].Value);
    ball = int.Parse(match.Groups[2].Value);
    return true;
}

}
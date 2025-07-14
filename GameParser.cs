using System.Text.RegularExpressions;

namespace CricketScoreReader;

using System;
using System.Collections.Generic;
using System.Linq;

public interface IGameParser
{
    List<BallResult> Results { get; }
    List<BallResult> History { get; }
    void ProcessFrameData(FrameData frameData, int frameNumber, DateTime timestamp);
}

public class GameParser : IGameParser
{
    private readonly object _lock = new();
    private readonly List<BallResult> _history = [];

    public List<BallResult> Results
    {
        get
        {
            var results = new List<BallResult>();
            lock (_lock)
            {
                int? lastRuns = null;
                int? lastWickets = null;
                var currentOver = 0;
                var currentBall = 0;


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
                    if (lastRuns == null || lastWickets == null)
                    {
                        lastRuns = runs;
                        lastWickets = wickets;
                        currentOver = over;
                        currentBall = ball;
                    }

                    var deltaRuns = Math.Max(0, runs - lastRuns.Value);
                    var deltaWickets = Math.Max(0, wickets - lastWickets.Value);
                    
                    

                    // Only add a new result if either over or ball has changed
                    if (over != currentOver || ball != currentBall || (over == currentOver && ball == currentBall && deltaRuns > 0))
                    {
                        results.Add(new BallResult
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

                        lastRuns = runs;
                        lastWickets = wickets;
                        currentOver = over;
                        currentBall = ball;
                    }
                    // if over or ball has not changed, handle wicket.
                    else if (wickets > lastWickets)
                    {
                        // delete last record and insert wicket record with last batter because wickets only change after player region changes(in next time scoreboard appears)
                        if (results.Count != 0)
                        {
                            results.RemoveAt(results.Count - 1);
                        }
                        results.Add(new BallResult
                        {
                            OverNumber = over,
                            BallNumber = ball,
                            Runs = deltaRuns,
                            Wickets = wickets - lastWickets.Value,
                            FrameNumber = frameNumber,
                            Timestamp = timestamp,
                            Batter1 = ballResults[i - 1].Batter1,
                            Batter2 = ballResults[i - 1].Batter2,
                            BattingTeam = ballResults[i - 1].BattingTeam,
                            Bowler = ballResults[i - 1].Bowler
                        });

                        lastWickets = wickets;
                    }
                }
            }

            return results;
        }
    }

    public List<BallResult> History
    {
        get
        {
            lock (_lock)
            {
                return _history.OrderBy(r => r.FrameNumber).ToList();
            }
        }
    }

    public void ProcessFrameData(FrameData frameData, int frameNumber, DateTime timestamp)
    {
        if (!TryParseScore(frameData.RunsWithWickets, out var runs, out var wickets)) return;
        if (!TryParseOvers(frameData.OversWithBalls, out var over, out var ball)) return;

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
        var match = Regex.Match(text, @"(\d+)\s*/\s*(\d+)");
        if (!match.Success) return false;

        runs = int.Parse(match.Groups[1].Value);
        wickets = int.Parse(match.Groups[2].Value);
        return true;
    }

    private static bool TryParseOvers(string text, out int over, out int ball)
    {
        over = ball = 0;
        var match = Regex.Match(text, @"(\d+)\.(\d+)");
        if (!match.Success) return false;

        over = int.Parse(match.Groups[1].Value);
        ball = int.Parse(match.Groups[2].Value);
        return true;
    }
}
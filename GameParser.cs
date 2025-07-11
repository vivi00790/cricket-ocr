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
        var runs = 0;
        var wickets = 0;

        switch (frameData.RunsWickets.Length)
        {
            case 1:
            case 2:
            case 3:
                if (frameData.Runs1Digit.Length==1)
                {
                    runs = int.Parse(frameData.Runs1Digit);
                    wickets = int.TryParse(frameData.Runs1DigitWickets, out var w) ? w : 9999;
                }
                else
                {
                    Console.WriteLine($"Unexpected runs label length: {JsonConvert.SerializeObject(frameData)}");
                    runs=9999; // Invalid runs, set to a sentinel value
                    wickets = 9999; // Invalid wickets, set to a sentinel value
                }
                break;
            case 4:
                if(frameData.Runs2Digits.Length==2)
                {
                    runs = int.Parse(frameData.Runs2Digits);
                    wickets = int.Parse(frameData.Runs2DigitsWickets);
                }
                else
                {
                    Console.WriteLine($"Unexpected runs label length: {JsonConvert.SerializeObject(frameData)}");
                    runs=9999; // Invalid runs, set to a sentinel value
                    wickets = 9999; // Invalid wickets, set to a sentinel value
                }
                break;
            case 5:
                if(frameData.Runs3Digits.Length==3)
                {
                    runs = int.Parse(frameData.Runs3Digits);
                    wickets = int.Parse(frameData.Runs3DigitsWickets);
                }
                else
                {
                    Console.WriteLine($"Unexpected runs label length: {JsonConvert.SerializeObject(frameData)}");
                    runs=9999; // Invalid runs, set to a sentinel value
                    wickets = 9999; // Invalid wickets, set to a sentinel value
                }
                break;
            default:
                Console.WriteLine($"Unexpected runs label length: {JsonConvert.SerializeObject(frameData)}");
                runs=9999; // Invalid runs, set to a sentinel value
                wickets = 9999; // Invalid wickets, set to a sentinel value
                return;
        }
        var over = int.TryParse(frameData.Over, out var o) ? o : 9999;
        var ball = int.TryParse(frameData.Ball, out var b) ? b : 9999;
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

}

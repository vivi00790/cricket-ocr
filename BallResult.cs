namespace CricketScoreReader;

public class BallResult
{
    public int OverNumber { get; set; }
    public int BallNumber { get; set; }
    public int Runs { get; set; }
    public int Wickets { get; set; }
    public int FrameNumber { get; set; }
    public DateTime Timestamp { get; set; }
    public string Batter1 { get; set; }
    public string Batter2 { get; set; }
    public string BattingTeam { get; set; }
    public string Bowler { get; set; }

    public override string ToString() =>
        $"Over {OverNumber}.{BallNumber} - Runs: {Runs}, Wickets: {Wickets}";
}

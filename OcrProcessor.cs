namespace CricketScoreReader;

using OpenCvSharp;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Tesseract;
using CvRect = OpenCvSharp.Rect;

public class OcrProcessor
{
    private readonly TesseractEngine _ocrNameEngine;
    private readonly Dictionary<string, CvRect> _roiMap;
    private readonly TesseractEngine _ocrOverBallEngine;
    private readonly TesseractEngine _ocrRunWicketEngine;
    private readonly TesseractEngine _ocrTeamEngine;

    public OcrProcessor(string tessdataPath, string roiConfigPath)
    {
        // player name and team name not using system dictionary for better accuracy
        _ocrNameEngine = new TesseractEngine(tessdataPath, "eng", EngineMode.Default);
        _ocrNameEngine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz. ");
        _ocrNameEngine.SetVariable("load_system_dawg", "F");
        _ocrNameEngine.SetVariable("load_freq_dawg", "F");
        _ocrTeamEngine = new TesseractEngine(tessdataPath, "eng", EngineMode.Default);
        _ocrTeamEngine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ");
        _ocrTeamEngine.SetVariable("load_system_dawg", "F");
        _ocrTeamEngine.SetVariable("load_freq_dawg", "F");
        _ocrRunWicketEngine = new TesseractEngine(tessdataPath, "eng", EngineMode.Default);
        _ocrRunWicketEngine.SetVariable("tessedit_char_whitelist", "01234567890/");
        // over.ball will not higher than 5
        _ocrOverBallEngine = new TesseractEngine(tessdataPath, "eng", EngineMode.Default);
        _ocrOverBallEngine.SetVariable("tessedit_char_whitelist", "0123456.");
        _roiMap = LoadRoiConfig(roiConfigPath);
    }

    public Dictionary<string, string> ProcessFrame(Mat frame)
    {
        var results = new Dictionary<string, string>();

        ProcessFrame(frame, _roiMap["BattingTeam"], "BattingTeam", results);

        if (!IsValidTeamName(results["BattingTeam"]))
        {
            return new Dictionary<string, string>();
        }

        foreach (var (label, roi) in _roiMap.Where(x => !x.Key.StartsWith("BattingTeam"))
                     .ToDictionary(x => x.Key, x => x.Value))
        {
            ProcessFrame(frame, roi, label, results);
        }

        return results;
    }

    private void ProcessFrame(Mat frame, CvRect roi, string label, Dictionary<string, string> results)
    {
        if (roi.X + roi.Width <= frame.Width && roi.Y + roi.Height <= frame.Height)
        {
            var roiMat = new Mat(frame, roi);
            if (roiMat.Empty())
            {
                Console.WriteLine($"ROI error frame={frame.Width}x{frame.Height}, roi={roi}");
                return;
            }

            // scale up and extend horizontally
            Cv2.Resize(roiMat, roiMat, new Size(0, 0), 1.5, 1.2, InterpolationFlags.Cubic);
            
            Cv2.CvtColor(roiMat, roiMat, ColorConversionCodes.BGR2GRAY);
            // apply blur, dilate, erode, sharpen, and threshold
            Cv2.MedianBlur(roiMat, roiMat, 3);
            Cv2.Dilate(roiMat, roiMat, Cv2.GetStructuringElement(MorphShapes.Rect, new Size(1, 1)));
            Cv2.MedianBlur(roiMat, roiMat, 3);
            Cv2.Erode(roiMat, roiMat, Cv2.GetStructuringElement(MorphShapes.Rect, new Size(1, 1)));
            Cv2.MedianBlur(roiMat, roiMat, 3);
            Cv2.Filter2D(roiMat, roiMat, -1,
                InputArray.Create(new[,] { { -1, -1, -1 }, { -1, 9, -1 }, { -1, -1, -1 } }));
            Cv2.MedianBlur(roiMat, roiMat, 3);
            Cv2.Threshold(roiMat, roiMat, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
            // invert the image to make text black on white background to match Tesseract's guideline
            Cv2.BitwiseNot(roiMat, roiMat);

            using var pix = MatToPix(roiMat);
            var page = label switch
            {
                "RunsWickets" => _ocrRunWicketEngine.Process(pix, PageSegMode.SingleLine),
                "OverWithBall" => _ocrOverBallEngine.Process(pix, PageSegMode.SingleLine),
                "BattingTeam" => _ocrTeamEngine.Process(pix, PageSegMode.SingleLine),
                _ => _ocrNameEngine.Process(pix, PageSegMode.SingleLine)
            };

            using (page)
            {
                var ocrResult = page.GetText().Trim();
                
                // check first player
                if (label is "Batter1" or "Batter2")
                {
                    var arrowRegion = new Mat(roiMat, new CvRect(0, 0, 20, roiMat.Rows));
                    var nonArrowPixels = Cv2.CountNonZero(arrowRegion);
                    // arrow ~ 80 pixels, arrow region ~ 840 pixels
                    var hasArrow = nonArrowPixels < arrowRegion.Rows * arrowRegion.Cols * 0.95;

                    // arrow sing first player
                    if (hasArrow)
                    {
                        results["Batter1"] = ocrResult;
                    }
                    else
                    {
                        results["Batter2"] = ocrResult;
                    }
                }
                else
                {
                    results[label] = ocrResult;
                }
            }
        }
    }

    private static Pix MatToPix(Mat roiMat)
    {
        Pix? pix = null;
        try
        {
            if (OperatingSystem.IsWindows()) // Tesseract Pix conversion only works on Windows with bitmap conversion
            {
                using var ms = new MemoryStream();
                using var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(roiMat);
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                ms.Position = 0;
                pix = Pix.LoadFromMemory(ms.ToArray());
                pix.XRes = 300;
                pix.YRes = 300;
                return pix;
            }

            throw new PlatformNotSupportedException("OCR Mat to Pix conversion currently only support under Windows");
        }
        catch
        {
            pix?.Dispose();
            throw;
        }
    }

    private static Dictionary<string, CvRect> LoadRoiConfig(string path)
    {
        var json = File.ReadAllText(path);
        var raw = JsonSerializer.Deserialize<Dictionary<string, RoiRect>>(json);
        var result = new Dictionary<string, CvRect>();
        foreach (var (key, roiRect) in raw)
        {
            result[key] = new CvRect(roiRect.X, roiRect.Y, roiRect.Width, roiRect.Height);
        }

        return result;
    }

    private class RoiRect
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    private static bool IsValidTeamName(string teamName)
    {
        return teamName.Length == 3 && teamName.Any(char.IsLetter) &&
               teamName.All(c => !char.IsLetter(c) || char.IsUpper(c));
    }
}
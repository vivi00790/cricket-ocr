namespace CricketScoreReader;


using OpenCvSharp;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Tesseract;
using CvRect = OpenCvSharp.Rect;

public class FrameProcessor
{
    private readonly TesseractEngine _ocrEngine;
    private readonly Dictionary<string, (bool IsScore, CvRect rect)> _roiMap;
    private readonly TesseractEngine _ocrRunEngine;
    private readonly TesseractEngine _ocrOverEngine;
    private readonly TesseractEngine _ocrBallEngine;
    private readonly TesseractEngine _ocrRunWicketEngine;
    private readonly TesseractEngine _ocrTeamEngine;

    public FrameProcessor(string tessdataPath, string roiConfigPath)
    {
        _ocrEngine = new TesseractEngine(tessdataPath, "eng", EngineMode.Default);
        _ocrEngine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz. ");
        _ocrEngine.SetVariable("load_system_dawg", "F");
        _ocrEngine.SetVariable("load_freq_dawg", "F");
        _ocrTeamEngine = new TesseractEngine(tessdataPath, "eng", EngineMode.Default);
        _ocrTeamEngine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ");
        _ocrTeamEngine.SetVariable("load_system_dawg", "F");
        _ocrTeamEngine.SetVariable("load_freq_dawg", "F");
        _ocrRunEngine = new TesseractEngine(tessdataPath, "eng", EngineMode.Default);
        _ocrRunEngine.SetVariable("tessedit_char_whitelist", "0123456789");
        _ocrRunWicketEngine = new TesseractEngine(tessdataPath, "eng", EngineMode.Default);
        _ocrRunWicketEngine.SetVariable("tessedit_char_whitelist", "0123456789/");
        _ocrOverEngine = new TesseractEngine(tessdataPath, "eng", EngineMode.Default);
        _ocrOverEngine.SetVariable("tessedit_char_whitelist", "01234");
        _ocrBallEngine = new TesseractEngine(tessdataPath, "eng", EngineMode.Default);
        _ocrBallEngine.SetVariable("tessedit_char_whitelist", "0123456.");
        _roiMap = LoadRoiConfig(roiConfigPath);
    }

    public Dictionary<string, string> ProcessFrame(Mat frame)
    {
        Console.WriteLine("Processing frame...");
        var results = new Dictionary<string, string>();

        foreach (var (label, (isScore, roi)) in _roiMap)
        {
            if (roi.X + roi.Width <= frame.Width && roi.Y + roi.Height <= frame.Height)
            {
                
                var roiMat = new Mat(frame, roi);
                if (roiMat.Empty())
                {
                    Console.WriteLine($"ROI error frame={frame.Width}x{frame.Height}, roi={roi}");
                    continue;
                }
                Cv2.Resize(roiMat, roiMat, new Size(0,0),1.5, 1.2, InterpolationFlags.Cubic); // 放大ROI以提高OCR準確率

                Cv2.CvtColor(roiMat, roiMat, ColorConversionCodes.BGR2GRAY); 
                
                if (true)
                {
                    Cv2.MedianBlur(roiMat, roiMat, 3);
                    Cv2.Dilate(roiMat, roiMat, Cv2.GetStructuringElement(MorphShapes.Rect, new Size(1, 1)));
                    Cv2.MedianBlur(roiMat, roiMat, 3);
                    Cv2.Erode(roiMat, roiMat, Cv2.GetStructuringElement(MorphShapes.Rect, new Size(1, 1)));
                    Cv2.MedianBlur(roiMat, roiMat, 3);
                    Cv2.Filter2D(roiMat, roiMat, -1, InputArray.Create(new[,] { {-1,-1,-1},{-1,9,-1},{-1,-1,-1} }));
                    Cv2.MedianBlur(roiMat, roiMat, 3);
                }

                Cv2.Threshold(roiMat, roiMat, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                Cv2.BitwiseNot(roiMat, roiMat);


                //Cv2.ImShow("ROI Preview", roiMat);
                //Cv2.WaitKey(0);
                using var pix = MatToPix(roiMat);
                var page = label switch
                {
                    "RunsWickets" => _ocrRunWicketEngine.Process(pix, PageSegMode.SingleLine),
                    "Runs1Digit" => _ocrRunEngine.Process(pix, PageSegMode.SingleChar),
                    "Runs2Digits" or "Runs3Digits" => _ocrRunEngine.Process(pix, PageSegMode.SingleChar),
                    "Runs1DigitWickets" or "Runs2DigitsWickets" or "Runs3DigitsWickets" => _ocrRunWicketEngine.Process(pix, PageSegMode.SingleChar),
                    "Over" => _ocrOverEngine.Process(pix, PageSegMode.SingleChar),
                    "Ball" or "OverWithBall" => _ocrBallEngine.Process(pix, PageSegMode.SingleLine),
                    "BattingTeam" => _ocrTeamEngine.Process(pix, PageSegMode.SingleLine),
                    _ => _ocrEngine.Process(pix, PageSegMode.SparseText)
                };

                using (page)
                {
                    var result = page.GetText().Trim();
                    Console.WriteLine(label+" OCR result:"+result);
                    results[label] = result;
                }
            }
        }

        return results;
    }

    private static Pix MatToPix(Mat roiMat)
    {
        Pix? pix = null;
        try
        {
            if (OperatingSystem.IsWindows())
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

    private Dictionary<string, (bool IsScore, CvRect rect)> LoadRoiConfig(string path)
    {
        var json = File.ReadAllText(path);
        var raw = JsonSerializer.Deserialize<Dictionary<string, RoiRect>>(json);
        var result = new Dictionary<string, (bool IsScore, CvRect rect)>();
        foreach (var kvp in raw)
        {
            var r = kvp.Value;
            result[kvp.Key] = (r.IsScore, new CvRect(r.X, r.Y, r.Width, r.Height));
        }
        return result;
    }

    private class RoiRect
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool IsScore { get; set; }
    }
}

namespace CricketScoreReader;


using OpenCvSharp;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.Json;
using Tesseract;
using CvRect = OpenCvSharp.Rect;

public class FrameProcessor
{
    private readonly TesseractEngine _ocrEngine;
    private readonly Dictionary<string, (bool IsScore, CvRect rect)> _roiMap;
    private readonly TesseractEngine _ocrScoreEngine;

    public FrameProcessor(string tessdataPath, string roiConfigPath)
    {
        _ocrEngine = new TesseractEngine(tessdataPath, "eng", EngineMode.Default);
        _ocrEngine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz. ");
        _ocrScoreEngine = new TesseractEngine(tessdataPath, "eng", EngineMode.Default);
        _ocrScoreEngine.SetVariable("tessedit_char_whitelist", "0123456789/. ");
        _roiMap = LoadRoiConfig(roiConfigPath);
    }

    public Dictionary<string, string> ProcessFrame(Mat frame)
    {
        Console.WriteLine("Processing frame...");
        var results = new Dictionary<string, string>();

        foreach (var kvp in _roiMap)
        {
            string label = kvp.Key;
            CvRect roi = kvp.Value.rect;

            if (roi.X + roi.Width <= frame.Width && roi.Y + roi.Height <= frame.Height)
            {
                
                Mat roiMat = new Mat(frame, roi);
                if (roiMat.Empty())
                {
                    Console.WriteLine($"ROI 寫入錯誤？frame={frame.Width}x{frame.Height}, roi={roi}");
                    continue;
                }

                Cv2.CvtColor(roiMat, roiMat, ColorConversionCodes.BGR2GRAY); 
                
                //Cv2.MedianBlur(roiMat, roiMat, 3); // 去除雜訊
                //Cv2.EqualizeHist(roiMat, roiMat);
                if (kvp.Value.IsScore)
                {
                    Cv2.Dilate(roiMat, roiMat, Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(1, 1))); // 擴大數字
                    Cv2.Filter2D(roiMat, roiMat, -1, InputArray.Create(new[,] { {-1,-1,-1},{-1,9,-1},{-1,-1,-1} })); // 銳化處理
                }

                Cv2.Threshold(roiMat, roiMat, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);


                Cv2.ImShow("ROI Preview", roiMat);
                Cv2.WaitKey(0);
                using var pix = MatToPix(roiMat);
                using var page = kvp.Value.IsScore ? 
                    _ocrScoreEngine.Process(pix,PageSegMode.SingleLine) :
                    _ocrEngine.Process(pix);
                var result = page.GetText().Trim();
                switch (label)
                {
                    case "RunsWickets":
                        if (!result.Contains("/") && result.Contains("7"))
                        {
                            Console.WriteLine($"RunsWickets Hardcoded, origin:{result}");
                            result = "0/0";
                        }
                        break;
                    case "Overs":
                        if (!result.Contains("."))
                        {
                            Console.WriteLine($"Overs Hardcoded, origin:{result}");
                            result = "0.0";
                        }
                        break;
                };
                Console.WriteLine(label+" OCR result:"+result);
                results[label] = result;
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

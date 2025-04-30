using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using FFMpegCore;

namespace VR2D;

public class Video(string input)
{
    public int HorizontalFieldOfView { get; set; } = 90;
    public int VerticalFieldOfView { get; set; } = 90;
    public int Yaw { get; set; }
    public int Pitch { get; set; }
    public string Input => input;

    public async Task<string> CreateScreenshot(TimeSpan startPosition)
    {
        const string fileName = "screenshot.png";
        var arguments =
            FFMpegArguments
                .FromFileInput(new FileInfo(input), options => options
                    .Seek(startPosition)
                )
                .OutputToFile(fileName, overwrite: true,
                    options => options
                        .WithConstantRateFactor(28)
                        .WithFrameOutputCount(1)
                        .WithVideoFilters(filter => filter
                            .VrTo2D(HorizontalFieldOfView, VerticalFieldOfView, Yaw, Pitch)
                            .Scale(new Size(-1, 720))
                        )
                );
        Console.WriteLine($"ffmpeg {arguments.Arguments}");
        var timer = new Stopwatch();
        timer.Start();
        await arguments.ProcessAsynchronously();
        timer.Stop();
        Console.WriteLine($"Saved to {fileName} in {timer.ElapsedMilliseconds}ms");
        return fileName;
    }

    public async Task<string> CreatePreview(TimeSpan startPosition, TimeSpan duration)
    {
        try
        {
            const string fileName = "preview.mp4";
            var arguments = FFMpegArguments
                .FromFileInput(new FileInfo(input), options => options
                    .Seek(startPosition)
                    .WithDuration(duration)
                ).OutputToFile(fileName, overwrite: true, options => options
                    .WithConstantRateFactor(28)
                    .WithFramerate(30)
                    .WithFastStart()
                    .ForcePixelFormat("yuv420p") // WMP can't handle 10-bit colors
                    .WithVideoFilters(filter => filter
                        .VrTo2D(HorizontalFieldOfView, VerticalFieldOfView, Yaw, Pitch)
                        .Scale(new Size(-1, 720))
                    ));

            Console.WriteLine($"ffmpeg {arguments.Arguments}");
            var timer = new Stopwatch();
            timer.Start();
            await arguments.ProcessAsynchronously();
            timer.Stop();
            Console.WriteLine($"Saved to {fileName} in {timer.ElapsedMilliseconds}ms");
            return fileName;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating video: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            throw;
        }
    }

    public string GetArguments(TimeSpan startPosition)
    {
        var arguments = FFMpegArguments
            .FromFileInput(new FileInfo(input), options => options
                .EndSeek(startPosition)
            ).OutputToFile("out.mp4", overwrite: true, options => options
                .WithConstantRateFactor(20)
                .WithFramerate(30)
                .WithFastStart()
                .WithVideoFilters(filter => filter
                    .VrTo2D(HorizontalFieldOfView, VerticalFieldOfView, Yaw, Pitch)
                    .Scale(new Size(-1, 1080))
                )
            );
        return $"ffmpeg {arguments.Arguments}";
    }

    public async Task<string> CreateTransitionFrom(Video initialParameters, TimeSpan endPosition)
    {
        var fileName = Path.Combine(Path.GetDirectoryName(input) ?? "", $"{Path.GetFileNameWithoutExtension(input)}-transition.mp4");
        const int millisecondsPerStep = 50;
        const int partCount = 1000 / millisecondsPerStep;
        var startPosition = endPosition.Subtract(TimeSpan.FromSeconds(1));
        string[] parts = [];

        var pitchDelta = (decimal)(Pitch -initialParameters.Pitch ) / (partCount + 1);
        var yawDelta = (decimal)(Yaw-initialParameters.Yaw ) / (partCount + 1);
        var hFoVDelta =(decimal)(  HorizontalFieldOfView-initialParameters.HorizontalFieldOfView) / (partCount + 1);
        var vFoVDelta = (decimal)( VerticalFieldOfView -initialParameters.VerticalFieldOfView) / (partCount + 1);
        var timer = new Stopwatch();
        timer.Start();
        for (var i = 0; i < partCount; i++)
        {
            var partName = $"transition-part-{i}.mp4";
            parts = parts.Append(Path.GetFullPath(partName)).ToArray();
            var seek = startPosition;
            startPosition = seek.Add(TimeSpan.FromMilliseconds(millisecondsPerStep));
            
            var targetHFoV = Math.Round(initialParameters.HorizontalFieldOfView + hFoVDelta * (i+1), 3);
            var targetVFoV = Math.Round(initialParameters.VerticalFieldOfView + vFoVDelta * (i+1), 3);
            var targetYaw =  Math.Round(initialParameters.Yaw + yawDelta * (i+1), 3);
            var targetPitch =  Math.Round(initialParameters.Pitch + pitchDelta * (i+1), 3);
            var arguments = FFMpegArguments.FromFileInput(new FileInfo(input), options => options
                .Seek(seek)
                .EndSeek(seek.Add(TimeSpan.FromMilliseconds(millisecondsPerStep)))
            ).OutputToFile(partName, overwrite: true, options => options
                .WithConstantRateFactor(20)
                .WithFastStart()
                .WithFramerate(30)
                .WithVideoFilters(filter => filter
                    .VrTo2D(targetHFoV, targetVFoV, targetYaw, targetPitch)
                    .Scale(new Size(-1, 1080))
                )
            );
            Console.WriteLine($"ffmpeg {arguments.Arguments}");
            await arguments.ProcessAsynchronously();
        }
        
        var concatArgs = FFMpegArguments.FromDemuxConcatInput(parts).OutputToFile(fileName, overwrite: true, options => options.WithCustomArgument("-c copy"));
        Console.WriteLine($"ffmpeg {concatArgs.Arguments}");
        await concatArgs.ProcessAsynchronously();
        timer.Stop();
        Console.WriteLine($"Saved to {fileName} in {timer.ElapsedMilliseconds}ms");
        foreach (var partName in parts)
        {
            File.Delete(partName);
        }
        return fileName;
    }
}
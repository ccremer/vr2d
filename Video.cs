using System.Diagnostics;
using System.Drawing;
using System.IO;
using FFMpegCore;

namespace VR2D;

public class Video(string input)
{
    public int HorizontalFieldOfView { get; set; } = 90;
    public int VerticalFieldOfView { get; set; } = 90;
    public int Yaw { get; set; }
    public int Pitch { get; set; }

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
}
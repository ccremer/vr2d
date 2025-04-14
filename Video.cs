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
                        .WithConstantRateFactor(22)
                        .WithFramerate(30)
                        .WithFrameOutputCount(1)
                        .WithVideoFilters(filter => filter
                            .VrTo2D(HorizontalFieldOfView, VerticalFieldOfView, Yaw, Pitch)
                            .Scale(new Size(-1, 720))
                        )
                );
        Console.WriteLine($"ffmpeg {arguments.Arguments}");
        await arguments.ProcessAsynchronously();
        Console.WriteLine($"Saved to {fileName}");
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
                    .WithConstantRateFactor(22)
                    .WithFramerate(30)
                    .WithFastStart()
                    .WithVideoFilters(filter => filter
                        .VrTo2D(HorizontalFieldOfView, VerticalFieldOfView, Yaw, Pitch)
                        .Scale(new Size(-1, 720))
                    ));

            Console.WriteLine($"ffmpeg {arguments.Arguments}");
            await arguments.ProcessAsynchronously();
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
                .Seek(startPosition)
            ).OutputToFile("out.mp4", overwrite: false, options => options
                .WithConstantRateFactor(22)
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
using System.IO;
using FFMpegCore;

namespace VR2D;

public class Video(string input)
{
    public int HorizontalFieldOfView { get; set; } = 90;
    public int VerticalFieldOfView { get; set; } = 90;
    public int Yaw { get; set; }
    public int Pitch { get; set; }
    
    private string? SampleFileName { get; set; }

    public async Task<string> CreateScreenshot()
    {
        const string fileName = "preview.png";
        var arguments =
            FFMpegArguments
                .FromFileInput(new FileInfo(SampleFileName ?? input)) 
                .OutputToFile(fileName, overwrite: true,
                    options => options
                        .WithConstantRateFactor(22)
                        .WithFrameOutputCount(1)
                        .WithCustomArgument(BuildVideoFilterArguments(720))
                    );
        Console.WriteLine($"ffmpeg {arguments.Arguments}");
        await arguments.ProcessAsynchronously();
        Console.WriteLine($"Saved to {fileName}");
        return fileName;
    }

    public async Task<VideoResult> CreateSample(TimeSpan startPosition)
    {
        try
        {
            SampleFileName = null;
            const string fileName = "sample.mp4";
            var arguments = FFMpegArguments
                .FromFileInput(new FileInfo(input), options => options
                    .Seek(startPosition)
                    .WithDuration(TimeSpan.FromSeconds(1))
                ).OutputToFile(fileName, overwrite: true, options => options
                    .WithFastStart()
                );

            Console.WriteLine($"ffmpeg {arguments.Arguments}");
            await arguments.ProcessAsynchronously();
            SampleFileName = fileName;
            return new VideoResult(true, null);
        } catch (Exception ex)
        {
            Console.WriteLine($"Error creating sample: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return new VideoResult(false, ex.StackTrace);
        }
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
                    .WithCustomArgument(
                        BuildVideoFilterArguments(720)));

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

    private string BuildVideoFilterArguments(int scale = 1080)
    {
        return $"-vf \"v360=input=equirect:output=flat:ih_fov=180:iv_fov=180:" +
               $"h_fov={HorizontalFieldOfView}:v_fov={VerticalFieldOfView}:" +
               $"in_stereo=sbs:yaw={Yaw}:pitch={Pitch},scale=-1:{scale}\"";
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
                .WithCustomArgument(
                    BuildVideoFilterArguments(1080))
            );
        return $"ffmpeg {arguments.Arguments}";
    }
}

public record VideoResult(bool Success, string? Message);
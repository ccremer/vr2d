using System.IO;
using System.Windows.Media.Imaging;
using FFMpegCore;
using FFMpegCore.Pipes;

namespace VR2D;

public class Video(string input)
{
    public int HorizontalFieldOfView { get; set; } = 90;
    public int VerticalFieldOfView { get; set; } = 90;
    public int Yaw { get; set; } = 0;
    public int Pitch { get; set; }

    public async Task<string> CreateScreenshot(TimeSpan position)
    {
        const string fileName = "preview.png";
        var arguments =
            FFMpegArguments
                .FromFileInput(new FileInfo(input), options => options
                    .Seek(position)
                )
                .OutputToFile(fileName, overwrite: true,
                    options => options
                        .WithConstantRateFactor(22)
                        .WithFastStart()
                        .WithCustomArgument(BuildVideoFilterArguments())
                        .WithCustomArgument("-frames:v 1")
                    );
        Console.WriteLine($"ffmpeg {arguments.Arguments}");
        await arguments.ProcessAsynchronously();
        Console.WriteLine("Done");
        return fileName;
    }

    public async Task<string> CreatePreview(TimeSpan startPosition, TimeSpan duration, int width = 320)
    {
        try
        {
            const string fileName = "test.mp4";
            var arguments = FFMpegArguments
                .FromFileInput(new FileInfo(input), options => options
                    .Seek(startPosition)
                    .WithDuration(duration)
                ).OutputToFile(fileName, overwrite: true, options => options
                    .WithConstantRateFactor(22)
                    .WithFramerate(30)
                    .WithFastStart()
                    .WithCustomArgument(
                        BuildVideoFilterArguments()));

            Console.WriteLine($"ffmpeg {arguments.Arguments}");
            await arguments.ProcessAsynchronously();
            Console.WriteLine("Done");
            return fileName;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating video: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            throw;
        }
    }

    public string BuildVideoFilterArguments()
    {
        return $"-vf \"v360=input=equirect:output=flat:ih_fov=180:iv_fov=180:" +
               $"h_fov={HorizontalFieldOfView}:v_fov={VerticalFieldOfView}:" +
               $"in_stereo=sbs:yaw={Yaw}:pitch={Pitch},scale=-1:1080\"";
    }
}
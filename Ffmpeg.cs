using System.IO;
using System.Windows.Media.Imaging;
using FFMpegCore;
using FFMpegCore.Pipes;

namespace VR2D;

public class Ffmpeg(String input)
{
    public int Pitch { get; set; }
    public int HorizontalFieldOfView { get; set; }
    public int VerticalFieldOfView { get; set; }

    public async Task CreateSnapshot()
    {
        var arguments =
            FFMpegArguments
                .FromFileInput(new FileInfo(input), options => options
                    .Seek(TimeSpan.FromMinutes(2))
                    .EndSeek(TimeSpan.FromMinutes(2).Add(TimeSpan.FromSeconds(5)))
                    .WithFramerate(30)
                )
                .OutputToFile("test.gif", overwrite: true,
                    options => options
                        .WithConstantRateFactor(22)
                        .WithFastStart());
        Console.WriteLine($"ffmpeg {arguments.Arguments}");
        await arguments.ProcessAsynchronously();
        Console.WriteLine("Done");
    }

    public async Task<BitmapImage> CreateGifInMemoryAsync(TimeSpan startPosition, TimeSpan duration, int width = 320)
    {
        try
        {
            // Use FFMpegCore to process the video segment into a GIF directly in memory
            var arguments = FFMpegArguments
                .FromFileInput(new FileInfo(input), options => options
                    .Seek(startPosition)
                    .WithDuration(duration)
                    .WithFramerate(30))
                .OutputToFile("test.gif", overwrite: true, options => options
          
                    .WithFramerate(10));

            Console.WriteLine($"ffmpeg {arguments.Arguments}");
            await arguments.ProcessAsynchronously();
            Console.WriteLine("Done");
            var finalImage = new BitmapImage();
            finalImage.BeginInit();
            finalImage.CacheOption = BitmapCacheOption.OnLoad;
            finalImage.UriSource = new Uri("test.gif", UriKind.Relative);
            finalImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            finalImage.EndInit();

            return finalImage;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating GIF in memory: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            throw;
        }
    }
}
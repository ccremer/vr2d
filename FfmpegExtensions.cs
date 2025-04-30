using FFMpegCore.Arguments;

namespace VR2D;

public static class FfmpegExtensions
{
    public static VideoFilterOptions VrTo2D(this VideoFilterOptions options, decimal horizontalFieldOfView,
        decimal verticalFieldOfView, decimal yaw = 0, decimal pitch = 0)
    {
        options.Arguments.Add(new VrVideoFilterArgument(horizontalFieldOfView, verticalFieldOfView, yaw, pitch));
        return options;
    }
}
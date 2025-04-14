using FFMpegCore.Arguments;

namespace VR2D;

public static class FfmpegExtensions
{
    public static VideoFilterOptions VrTo2D(this VideoFilterOptions options, int horizontalFieldOfView,
        int verticalFieldOfView, int yaw = 0, int pitch = 0)
    {
        options.Arguments.Add(new VrVideoFilterArgument(horizontalFieldOfView, verticalFieldOfView, yaw, pitch));
        return options;
    }
}
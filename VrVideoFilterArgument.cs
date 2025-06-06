﻿using FFMpegCore.Arguments;

namespace VR2D;

public class VrVideoFilterArgument(decimal horizontalFieldOfView, decimal verticalFieldOfView, decimal yaw = 0, decimal pitch = 0) : IVideoFilterArgument{
    
    public string Key => "v360";

    public string Value => $"input=equirect:output=flat:ih_fov=180:iv_fov=180:" +
                           $"h_fov={horizontalFieldOfView}:v_fov={verticalFieldOfView}:" +
                           $"in_stereo=sbs:yaw={yaw}:pitch={pitch}";
}